import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { OrderStatus } from '../../../../core/enums/order-status';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderService } from '../../../../core/services/order-service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, CreditCard, X, Printer } from 'lucide-angular';

@Component({
  selector: 'app-orders',
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
})
export class Orders implements OnInit {
  public readonly CreditCard = CreditCard;
  public readonly XIcon = X;
  public readonly Printer = Printer;
  private orderService = inject(OrderService);
  private router = inject(Router);

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

  applyFilters() {
    this.filteredOrders = this.recentOrders.filter((order) => {
      const currentStatus = OrderStatus[order.orderStatus];
      return this.filterStatus ? currentStatus === this.filterStatus : true;
    });
  }

  goToCheckout(order: OrderResponseDto) {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id } });
  }
}
