import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../../core/services/order-service';
import { Router } from '@angular/router';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderStatus } from '../../../../core/enums/order-status';
import { SignalrService } from '../../../../core/services/signalr-service';
import { PaymentStatus } from '../../../../core/enums/payment-status';

@Component({
  selector: 'app-close-orders',
  imports: [CommonModule, FormsModule],
  templateUrl: './close-orders.html',
  styleUrl: './close-orders.scss',
})
export class CloseOrders implements OnInit {
  private orderService = inject(OrderService);
  private router = inject(Router);
  private signalrService = inject(SignalrService);

  activeOrders: OrderResponseDto[] = [];
  filteredOrders: OrderResponseDto[] = [];
  selectedOrder: OrderResponseDto | null = null;

  searchTerm: string = '';

  constructor() {
    effect(() => {
      const readyItem = this.signalrService.itemReadySignal();
      const newItem = this.signalrService.newItemSignal();

      if (readyItem || newItem) {
        this.loadActiveOrders();
      }
    });
  }

  ngOnInit() {
    this.signalrService.startConnection();
    this.loadActiveOrders();
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

  loadActiveOrders() {
    this.orderService.getAll().subscribe({
      next: (data) => {
        this.activeOrders = data.filter(
          (order) =>
            order.paymentStatus !== PaymentStatus.Paid &&
            OrderStatus[order.orderStatus] !== 'Canceled',
        );
        this.applyFilters();
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

  goBack() {
    this.router.navigate(['/pdv']);
  }

  goToCheckout(order: OrderResponseDto) {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id } });
  }
}
