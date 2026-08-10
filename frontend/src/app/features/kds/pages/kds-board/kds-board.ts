import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { LucideAngularModule, Rows3, History, Funnel } from 'lucide-angular';
import { SignalrService } from '../../../../core/services/signalr-service';
import { KdsService } from '../../../../core/services/kds-service';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { KdsStatus } from '../../../../core/enums/kds-status';
import { OrderStatus } from '../../../../core/enums/order-status';
import { Subscription } from 'rxjs/internal/Subscription';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-kds-board',
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './kds-board.html',
  styleUrl: './kds-board.scss',
})
export class KdsBoard implements OnInit, OnDestroy {
  readonly Rows3 = Rows3;
  readonly Funnel = Funnel;
  readonly History = History;

  private kdsService = inject(KdsService);
  private signalrService = inject(SignalrService);

  private notificationSound = new Audio('/notificationSound.mp3');

  activeOrders: OrderResponseDto[] = [];

  public readonly KdsStatus = KdsStatus;
  public readonly OrderStatus = OrderStatus;

  private subscriptions = new Subscription();

  ngOnInit(): void {
    this.loadPendingItems();
    this.signalrService.startConnection();

    this.subscriptions.add(
      this.signalrService.orderUpdated$.subscribe((updatedOrder) => {
        this.handleOrderUpdate(updatedOrder);
      }),
    );

    this.subscriptions.add(
      this.signalrService.orderDelivered$.subscribe((deliveredOrder) => {
        this.removeOrderFromScreen(deliveredOrder.id);
      }),
    );
  }

  ngOnDestroy(): void {
    this.signalrService.stopConnection();
    this.subscriptions.unsubscribe();
  }

  loadPendingItems(): void {
    this.kdsService.getPendingKdsOrders().subscribe({
      next: (orders) => {
        this.activeOrders = orders;
      },
      error: (err) => console.error('Erro ao carregar comandas do KDS', err),
    });
  }

  markItemAsDone(orderId: string, itemId: string): void {
    const order = this.activeOrders.find((o) => o.id === orderId);
    if (order) {
      const item = order.items.find((i) => i.id === itemId);
      if (item) item.kdsStatus = KdsStatus.Done;
    }

    this.kdsService.updateItemStatus(orderId, itemId, KdsStatus.Done).subscribe({
      next: () => {},
      error: (err) => {
        console.error('Erro ao atualizar item na cozinha:', err);
      },
    });
  }

  markOrderAsDone(order: OrderResponseDto): void {
    const pendingItems = order.items.filter((i) => i.kdsStatus !== KdsStatus.Done);
    if (pendingItems.length === 0) return;

    this.removeOrderFromScreen(order.id);

    const requests = pendingItems.map((item) =>
      this.kdsService.updateItemStatus(order.id, item.id, KdsStatus.Done),
    );

    forkJoin(requests).subscribe({
      next: () => {
        console.log(`Comanda #${order.id.substring(0, 6)} finalizada na API!`);
      },
      error: (err) => {
        console.error('Erro ao finalizar comanda na API:', err);
        this.loadPendingItems();
      },
    });
  }

  handleOrderUpdate(updatedOrder: OrderResponseDto): void {
    const shouldRemove =
      updatedOrder.orderStatus === OrderStatus.ReadyToServe ||
      updatedOrder.orderStatus === OrderStatus.Delivered ||
      updatedOrder.orderStatus === OrderStatus.Canceled;

    if (shouldRemove) {
      this.removeOrderFromScreen(updatedOrder.id);
    } else {
      const existingIndex = this.activeOrders.findIndex((o) => o.id === updatedOrder.id);
      if (existingIndex !== -1) {
        this.activeOrders[existingIndex] = updatedOrder;
      } else {
        this.activeOrders.push(updatedOrder);
        this.playSound();
      }
      this.activeOrders.sort(
        (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
      );
    }
  }

  removeOrderFromScreen(orderId: string): void {
    this.activeOrders = this.activeOrders.filter((o) => o.id !== orderId);
  }

  private playSound(): void {
    this.notificationSound.currentTime = 0;

    this.notificationSound.play().catch((error) => {
      console.warn(
        'O navegador bloqueou o áudio automático. O usuário precisa clicar na tela primeiro.',
        error,
      );
    });
  }
}
