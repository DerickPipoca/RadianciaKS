import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnInit, ViewChild } from '@angular/core';
import { OrderStatus } from '../../../../core/enums/order-status';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderService } from '../../../../core/services/order-service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, CreditCard, X, Printer } from 'lucide-angular';
import { SignalrService } from '../../../../core/services/signalr-service';
import { PaymentStatus } from '../../../../core/enums/payment-status';
import { PrintPreview } from '../../components/print-preview/print-preview';

@Component({
  selector: 'app-orders',
  imports: [CommonModule, FormsModule, LucideAngularModule, PrintPreview],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
})
export class Orders implements OnInit {
  public readonly CreditCard = CreditCard;
  public readonly XIcon = X;
  public readonly Printer = Printer;
  private orderService = inject(OrderService);
  private router = inject(Router);
  private signalrService = inject(SignalrService);

  isPrinting = false;

  @ViewChild(PrintPreview) printPreview!: PrintPreview;

  constructor() {
    effect(() => {
      const readyItem = this.signalrService.itemReadySignal();
      const newItem = this.signalrService.newItemSignal();

      if (readyItem || newItem) {
        this.loadOrders();
      }
    });
  }

  getOrderStatusLabel(order: OrderResponseDto) {
    return OrderStatus[order.orderStatus] || 'Desconhecido';
  }
  recentOrders: OrderResponseDto[] = [];
  filteredOrders: OrderResponseDto[] = [];

  filterStatus: string = '';
  availableStatuses = ['Open', 'ReadyToServe', 'Paid', 'Canceled'];

  selectedOrder: OrderResponseDto | null = null;

  ngOnInit() {
    this.loadOrders();
    this.signalrService.startConnection();
  }

  loadOrders() {
    this.orderService.getAll().subscribe({
      next: (data) => {
        this.recentOrders = data;
        this.filteredOrders = data;
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
          this.applyFilters();
          this.closeDetails();
        },
        error: (err) => {
          console.error('Erro ao cancelar pedido:', err);
          alert('Não foi possível cancelar o pedido. Tente novamente.');
        },
      });
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

  applyFilters() {
    this.filteredOrders = this.recentOrders.filter((order) => {
      if (!this.filterStatus) return true;

      if (this.filterStatus === 'Paid') {
        return order.paymentStatus === PaymentStatus.Paid;
      } else {
        return OrderStatus[order.orderStatus] === this.filterStatus;
      }
    });
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
            /* Adicione aqui qualquer estilo CSS que o seu recibo precise */
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
