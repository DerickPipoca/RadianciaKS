import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { OrderStatus } from '../../../../core/enums/order-status';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderService } from '../../../../core/services/order-service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, CreditCard, X, Printer, TextSearch } from 'lucide-angular';
import { SignalrService } from '../../../../core/services/signalr-service';
import { PaymentStatus } from '../../../../core/enums/payment-status';
import { PrintPreview } from '../../components/print-preview/print-preview';
import { debounceTime, distinctUntilChanged, merge, Subject, Subscription } from 'rxjs';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import {
  OrderStatusClassPipe,
  OrderStatusLabelPipe,
} from '../../../../core/pipes/order-status-pipe-pipe';
import {
  PaymentStatusClassPipe,
  PaymentStatusLabelPipe,
} from '../../../../core/pipes/payment-status-pipe-pipe';
import { ClickOutsideDirective } from '../../../../core/directives/click-outside-directive';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ToastrService } from 'ngx-toastr';
import { PrintService } from '../../../../core/services/print-service';

@Component({
  selector: 'app-orders',
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    PrintPreview,
    InputComponent,
    OrderStatusClassPipe,
    PaymentStatusClassPipe,
    OrderStatusLabelPipe,
    PaymentStatusLabelPipe,
    ClickOutsideDirective,
    Pagination,
    ModalComponent,
  ],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
})
export class Orders implements OnInit, OnDestroy {
  public readonly OrderStatus = OrderStatus;
  public readonly PaymentStatus = PaymentStatus;
  public readonly CreditCard = CreditCard;
  public readonly XIcon = X;
  public readonly Printer = Printer;
  public readonly TextSearch = TextSearch;

  private orderService = inject(OrderService);
  private router = inject(Router);
  private signalrService = inject(SignalrService);
  private toastr = inject(ToastrService);
  private printService = inject(PrintService);

  isPrinting = false;

  searchTerm: string = '';
  searchSubject = new Subject<string>();
  currentSortColumn: string = 'createdAt';
  isDescending: boolean = true;

  pageNumber = 1;
  pageSize = 12;
  totalRecords = 0;

  dropdownOpen = false;

  private subscriptions = new Subscription();

  @ViewChild(PrintPreview) printPreview!: PrintPreview;

  recentOrders: OrderResponseDto[] = [];
  filteredOrders: OrderResponseDto[] = [];

  filterStatus: string = '';
  availableStatuses = ['Open', 'ReadyToServe', 'Delivered', 'Paid', 'Canceled'];

  selectedOrder: OrderResponseDto | null = null;

  constructor() {
    this.subscriptions.add(
      this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
        this.pageNumber = 1;
        this.loadOrders();
      }),
    );
  }

  ngOnInit() {
    this.loadOrders();
    this.signalrService.startConnection();

    this.subscriptions.add(
      merge(
        this.signalrService.orderUpdated$,
        this.signalrService.orderDelivered$,
        this.signalrService.orderCanceled$,
      )
        .pipe(debounceTime(300))
        .subscribe(() => {
          this.loadOrders();
        }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  changeFilterStatus(status: string) {
    this.filterStatus = status;
    this.onStatusChange();
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

  loadOrders() {
    let statusEnum: OrderStatus | undefined = undefined;
    let paymentStatus: PaymentStatus | undefined = undefined;
    if (this.filterStatus === 'Paid') {
      paymentStatus = PaymentStatus.Paid;
    } else if (this.filterStatus) {
      statusEnum = this.filterStatus ? (OrderStatus[this.filterStatus as any] as any) : null;
    }

    const params = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm,
      sortBy: this.currentSortColumn,
      isDescending: this.isDescending,
      status: statusEnum,
      paymentStatus: paymentStatus,
    };
    this.orderService.getAll(params).subscribe({
      next: (response) => {
        this.recentOrders = response.data;
        this.filteredOrders = response.data;

        this.totalRecords = response.totalRecords;
      },
      error: (err) => console.error('Erro ao buscar pedidos na API:', err),
    });
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

  get rangeStart(): number {
    return (this.pageNumber - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalRecords);
  }

  get pageNumbers(): number[] {
    const total = this.getTotalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  goBack() {
    this.router.navigate(['/pdv']);
  }

  cancelOrder(order: OrderResponseDto) {
    if (
      confirm(
        `Tem certeza que deseja cancelar o pedido #${order.id.substring(0, 6).toUpperCase()}?`,
      )
    ) {
      this.orderService.cancelOrder(order.id).subscribe({
        next: () => {
          order.orderStatus = OrderStatus.Canceled as any;
          this.closeDetails();
          this.loadOrders();
          this.toastr.success('Pedido cancelado com sucesso!');
        },
        error: (err) => {
          console.error('Erro ao cancelar pedido:', err);
          this.toastr.error('Não foi possível cancelar o pedido. Tente novamente.');
        },
      });
    }
  }

  deliverOrder(order: OrderResponseDto) {
    if (
      confirm(
        `Confirmar a entrega do pedido #${order.id.substring(0, 6).toUpperCase()} ao cliente?`,
      )
    ) {
      this.orderService.deliverOrder(order.id).subscribe({
        next: () => {
          order.orderStatus = OrderStatus.Delivered as any;
          this.closeDetails();
          this.loadOrders();
          this.toastr.success('Entrega registrada com sucesso!');
        },
        error: (err) => {
          console.error('Erro ao entregar pedido:', err);
          this.toastr.error('Não foi possível registrar a entrega. Tente novamente.');
        },
      });
    }
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  goToCheckout(order: OrderResponseDto) {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id } });
  }

  confirmPrint() {
    this.printService.printElement('print-section', 'Imprimir Comanda');
  }
}
