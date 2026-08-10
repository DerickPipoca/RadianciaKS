import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../../core/services/order-service';
import { Router } from '@angular/router';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderStatus } from '../../../../core/enums/order-status';
import { SignalrService } from '../../../../core/services/signalr-service';
import { PaymentStatus } from '../../../../core/enums/payment-status';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { LucideAngularModule, TextSearch } from 'lucide-angular';
import { debounceTime, distinctUntilChanged, merge, Subject, Subscription } from 'rxjs';

@Component({
  selector: 'app-close-orders',
  imports: [CommonModule, FormsModule, InputComponent, LucideAngularModule],
  templateUrl: './close-orders.html',
  styleUrl: './close-orders.scss',
})
export class CloseOrders implements OnInit, OnDestroy {
  public TextSearch = TextSearch;
  currentSortColumn: string = 'createdAt';
  isDescending: boolean = true;

  private orderService = inject(OrderService);
  private router = inject(Router);
  private signalrService = inject(SignalrService);

  activeOrders: OrderResponseDto[] = [];
  filteredOrders: OrderResponseDto[] = [];
  selectedOrder: OrderResponseDto | null = null;

  pageNumber = 1;
  pageSize = 60;
  totalRecords = 0;

  searchTerm: string = '';
  searchSubject = new Subject<string>();

  private subscriptions = new Subscription();

  constructor() {
    this.subscriptions.add(
      this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
        this.pageNumber = 1;
        this.loadOrders();
      }),
    );
  }

  changeSort(column: string) {
    if (this.currentSortColumn === column) {
      this.isDescending = !this.isDescending;
    } else {
      this.currentSortColumn = column;
      this.isDescending = true;
    }
    this.pageNumber = 1;
    this.loadOrders();
  }

  ngOnInit() {
    this.signalrService.startConnection();
    this.loadOrders();

    this.subscriptions.add(
      merge(this.signalrService.orderUpdated$, this.signalrService.orderDelivered$)
        .pipe(debounceTime(300))
        .subscribe(() => {
          this.loadOrders();
        }),
    );
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  getPaymentStatusLabel(order: OrderResponseDto): string {
    switch (order.paymentStatus) {
      case PaymentStatus.Pending:
        return 'Pag. Pendente';
      case PaymentStatus.Partial:
        return 'Pago Parcial';
      case PaymentStatus.Paid:
        return 'Pago';
      case PaymentStatus.Refunded:
        return 'Estornado';
      default:
        return 'Desconhecido';
    }
  }

  getPaymentStatusClass(order: OrderResponseDto): string {
    switch (order.paymentStatus) {
      case PaymentStatus.Pending:
        return 'pending';
      case PaymentStatus.Partial:
        return 'partial';
      case PaymentStatus.Paid:
        return 'paid';
      case PaymentStatus.Refunded:
        return 'refunded';
      default:
        return 'pending';
    }
  }

  loadOrders() {
    const params = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm,
      sortBy: this.currentSortColumn,
      isDescending: this.isDescending,
      paymentStatus: PaymentStatus.Pending,
    };
    this.orderService.getAll(params).subscribe({
      next: (response) => {
        this.activeOrders = response.data;
        this.filteredOrders = response.data;
        this.totalRecords = response.totalRecords;
      },
      error: (err) => console.error('Erro ao buscar contas em aberto:', err),
    });
  }

  applyFilters() {
    this.filteredOrders = this.activeOrders.filter((order) => {
      if (!this.searchTerm) return true;
      const term = this.searchTerm.toLowerCase();
      const tableInfo = order.tableNumber ? order.tableNumber.toLowerCase() : 'balcão';
      const orderId = order.id.toLowerCase();

      return tableInfo.includes(term) || orderId.includes(term);
    });
  }

  getOrderStatusLabel(order: OrderResponseDto): string {
    if (order.paymentStatus === PaymentStatus.Paid) return 'Pago';
    if (order.orderStatus === OrderStatus.ReadyToServe) return 'Pronto para Servir';
    if (order.orderStatus === OrderStatus.Preparing) return 'Na Cozinha';
    if (order.orderStatus === OrderStatus.Open) return 'Em Aberto';
    return 'Desconhecido';
  }

  getStatusClass(order: OrderResponseDto): string {
    if (order.paymentStatus === PaymentStatus.Paid) return 'paid';
    if (order.orderStatus === OrderStatus.ReadyToServe) return 'ready-to-serve';
    return 'open';
  }

  openDetails(order: OrderResponseDto) {
    this.selectedOrder = order;
  }

  closeDetails() {
    this.selectedOrder = null;
  }

  changePage(newPage: number) {
    this.pageNumber = newPage;
    this.loadOrders();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalRecords / this.pageSize);
  }

  onStatusChange() {
    this.pageNumber = 1;
    this.searchTerm = '';
    this.loadOrders();
  }

  goBack() {
    this.router.navigate(['/pdv']);
  }

  goToCheckout(order: OrderResponseDto) {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id } });
  }
}
