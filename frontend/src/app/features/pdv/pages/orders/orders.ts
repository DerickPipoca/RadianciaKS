import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
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

@Component({
  selector: 'app-orders',
  imports: [CommonModule, FormsModule, LucideAngularModule, PrintPreview, InputComponent],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
})
export class Orders implements OnInit, OnDestroy {
  public readonly OrderStatus = OrderStatus;
  public readonly CreditCard = CreditCard;
  public readonly XIcon = X;
  public readonly Printer = Printer;
  public readonly TextSearch = TextSearch;
  private orderService = inject(OrderService);
  private router = inject(Router);
  private signalrService = inject(SignalrService);

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

  constructor() {
    this.subscriptions.add(
      this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
        this.pageNumber = 1;
        this.loadOrders();
      }),
    );
  }

  recentOrders: OrderResponseDto[] = [];
  filteredOrders: OrderResponseDto[] = [];

  filterStatus: string = '';
  availableStatuses = ['Open', 'ReadyToServe', 'Delivered', 'Paid', 'Canceled'];

  selectedOrder: OrderResponseDto | null = null;

  ngOnInit() {
    this.loadOrders();
    this.signalrService.startConnection();

    this.subscriptions.add(
      merge(
        this.signalrService.orderUpdated$,
        this.signalrService.orderDelivered$,
        this.signalrService.orderDelivered$,
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

  getStatusClass(order: OrderResponseDto): string {
    const statusName = OrderStatus[order.orderStatus];
    if (!statusName) return 'open';

    if (statusName === 'ReadyToServe') return 'ready-to-serve';
    if (statusName === 'Canceled') return 'canceled';
    if (statusName === 'Paid') return 'paid';

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
        },
        error: (err) => {
          console.error('Erro ao cancelar pedido:', err);
          alert('Não foi possível cancelar o pedido. Tente novamente.');
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
        },
        error: (err) => {
          console.error('Erro ao entregar pedido:', err);
          alert('Não foi possível registrar a entrega. Tente novamente.');
        },
      });
    }
  }

  getOrderStatusLabel(order: OrderResponseDto): string {
    switch (order.orderStatus) {
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
        return 'Desconhecido';
    }
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
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

  goToCheckout(order: OrderResponseDto) {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id } });
  }

  confirmPrint() {
    const printWindow = window.open('', '_blank', 'width=300,height=600');

    if (printWindow) {
      const content = document.getElementById('print-section')?.innerHTML;

      printWindow.document.write(`
      <html>
        <head>
          <title>Imprimir Comanda</title>
          <style>
            body { font-family: 'Courier New', monospace; font-size: 14px; margin: 0; padding: 10px; width: 80mm; }
            .receipt-container { width: 80mm; }
          </style>
        </head>
        <body>
          <div class="receipt-container">${content}</div>
        </body>
      </html>
    `);

      printWindow.document.close();
      printWindow.focus();
      setTimeout(() => {
        printWindow.print();
        printWindow.close();
      }, 100);
    }
  }
}
