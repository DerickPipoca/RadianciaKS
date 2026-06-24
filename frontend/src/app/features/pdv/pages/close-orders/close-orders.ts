import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../../core/services/order-service';
import { Router } from '@angular/router';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderStatus } from '../../../../core/enums/order-status';
import { SignalrService } from '../../../../core/services/signalr-service';

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
      if (readyItem) {
        this.loadActiveOrders();
      }
    });
  }

  ngOnInit() {
    this.signalrService.startConnection();
    this.loadActiveOrders();
  }

  loadActiveOrders() {
    this.orderService.getAll().subscribe({
      next: (data) => {
        this.activeOrders = data.filter(
          (order) =>
            OrderStatus[order.orderStatus] === 'Open' ||
            OrderStatus[order.orderStatus] === 'ReadyToServe',
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
  getOrderStatusLabel(order: OrderResponseDto) {
    return OrderStatus[order.orderStatus] || 'Desconhecido';
  }

  getStatusClass(order: OrderResponseDto): string {
    const statusName = OrderStatus[order.orderStatus];
    if (statusName === 'ReadyToServe') return 'ready-to-serve';
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
