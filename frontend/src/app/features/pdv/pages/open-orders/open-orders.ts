import { OrderResponseDto } from './../../../../core/models/order.model';
import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, inject, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule, Search, SquarePen, Plus } from 'lucide-angular';
import { OrderService } from '../../../../core/services/order-service';
import { CartService } from '../../../../core/services/cart-service';
import { OrderStatus } from '../../../../core/enums/order-status';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { PaymentStatus } from '../../../../core/enums/payment-status';
import { InputComponent } from '../../../../shared/components/input-component/input-component';

@Component({
  selector: 'app-open-orders',
  imports: [CommonModule, LucideAngularModule, InputComponent],
  templateUrl: './open-orders.html',
  styleUrl: './open-orders.scss',
})
export class OpenOrders {
  readonly Search = Search;
  readonly SquarePen = SquarePen;
  readonly Plus = Plus;

  PaymentStatus = PaymentStatus;

  searchTerm: string = '';
  searchSubject = new Subject<string>();
  currentSortColumn: string = 'createdAt';
  isDescending: boolean = true;

  constructor() {
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {});
  }

  private orderService = inject(OrderService);
  private cartService = inject(CartService);
  private router = inject(Router);

  public readonly OrderStatus = OrderStatus;

  dropdownOpen = false;
  filterStatus: string = '';
  availableStatuses = ['Open', 'ReadyToServe', 'Delivered', 'Paid', 'Canceled'];

  private allOrders: OrderResponseDto[] = [];
  selectedOrder: OrderResponseDto | null = null;

  @ViewChild('selectContainer') selectContainer!: ElementRef;

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    if (
      this.dropdownOpen &&
      this.selectContainer &&
      !this.selectContainer.nativeElement.contains(event.target)
    ) {
      this.dropdownOpen = false;
    }
  }

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    let statusEnum: OrderStatus | undefined = undefined;
    let paymentStatus: PaymentStatus | undefined = undefined;
    if (this.filterStatus === 'Paid') {
      paymentStatus = PaymentStatus.Paid;
    } else if (this.filterStatus) {
      statusEnum = this.filterStatus ? (OrderStatus[this.filterStatus as any] as any) : null;
    }

    this.orderService
      .getAll({
        pageNumber: 1,
        pageSize: 200,
        sortBy: this.currentSortColumn,
        isDescending: this.isDescending,
        status: statusEnum,
        paymentStatus: paymentStatus,
      })
      .subscribe({
        next: (res) => {
          this.allOrders = res.data;
        },
        error: (err) => console.error('Erro ao carregar pedidos', err),
      });
  }

  get activeOrders(): OrderResponseDto[] {
    return this.allOrders.filter((order) => {
      if (order.orderStatus === OrderStatus.Canceled) {
        return false;
      }

      if (
        order.paymentStatus === PaymentStatus.Paid &&
        order.orderStatus === OrderStatus.Delivered
      ) {
        return false;
      }
      return true;
    });
  }

  canCheckout(order: OrderResponseDto): boolean {
    return order.paymentStatus !== PaymentStatus.Paid;
  }

  openTableForEditing(order: OrderResponseDto) {
    this.cartService.loadOrderForEditing(order);
    this.router.navigate(['/pdv/catalogo']);
  }

  goBack(): void {
    this.router.navigate(['/pdv/home']);
  }

  openDetails(order: OrderResponseDto): void {
    this.selectedOrder = order;
  }

  closeDetails(): void {
    this.selectedOrder = null;
  }

  goToCheckout(order: OrderResponseDto): void {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id , abertos: true} });
  }

  changeSort(column: string): void {
    if (this.currentSortColumn === column) {
      this.isDescending = !this.isDescending;
    } else {
      this.currentSortColumn = column;
      this.isDescending = true;
    }
  }

  get filteredOrders(): OrderResponseDto[] {
    let filtered = this.activeOrders;

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(
        (o) =>
          (o.tableNumber && o.tableNumber.toLowerCase().includes(term)) ||
          o.id.toLowerCase().includes(term),
      );
    }

    return filtered.sort((a, b) => {
      let valA: any = a[this.currentSortColumn as keyof OrderResponseDto];
      let valB: any = b[this.currentSortColumn as keyof OrderResponseDto];

      if (this.currentSortColumn === 'createdAt') {
        valA = new Date(valA).getTime();
        valB = new Date(valB).getTime();
      }

      if (valA < valB) return this.isDescending ? 1 : -1;
      if (valA > valB) return this.isDescending ? -1 : 1;
      return 0;
    });
  }

  getOrderStatusLabel(order: OrderResponseDto): string {
    switch (order.orderStatus) {
      case OrderStatus.Preparing:
        return 'Preparando';
      case OrderStatus.ReadyToServe:
        return 'Pronto p/ servir';
      case OrderStatus.Delivered:
        return 'Entregue';
      case OrderStatus.Open:
        return 'Aberto';
      default:
        return 'Desconhecido';
    }
  }

  getStatusClass(order: OrderResponseDto): string {
    switch (order.orderStatus) {
      case OrderStatus.Open:
        return 'open';
      case OrderStatus.Preparing:
        return 'preparing';
      case OrderStatus.ReadyToServe:
        return 'ready-to-serve';
      case OrderStatus.Delivered:
        return 'delivered';
      default:
        return '';
    }
  }

  getPaymentStatusLabel(order: OrderResponseDto): string {
    switch (order.paymentStatus) {
      case PaymentStatus.Pending:
        return 'Pendente';
      case PaymentStatus.Partial:
        return 'Parcial';
      default:
        return 'Pendente';
    }
  }

  formattedOrderId(order: OrderResponseDto): string {
    if (!order || !order.id) return '';
    return String(order.id).slice(0, 6).toUpperCase();
  }

  getPaymentStatusClass(order: OrderResponseDto): string {
    switch (order.paymentStatus) {
      case PaymentStatus.Pending:
        return 'pending';
      case PaymentStatus.Partial:
        return 'partial';
      default:
        return 'pending';
    }
  }

  getOrderStatus(orderStatus: string): string | null {
    if (orderStatus === 'Paid') return 'Pago';
    let statusEnum = orderStatus ? (OrderStatus[orderStatus as any] as any) : null;
    switch (statusEnum) {
      case OrderStatus.Canceled:
        return 'Cancelado';
      case OrderStatus.Preparing:
        return 'Preparando';
      case OrderStatus.ReadyToServe:
        return 'Pronto p/ servir';
      case OrderStatus.Delivered:
        return 'Entregue';
      case OrderStatus.Open:
        return 'Aberto';
      default:
        return null;
    }
  }

  changeFilterStatus(status: string) {
    this.filterStatus = status;
    this.onStatusChange();
  }

  onStatusChange() {
    this.searchTerm = '';
    this.loadOrders();
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }
}
