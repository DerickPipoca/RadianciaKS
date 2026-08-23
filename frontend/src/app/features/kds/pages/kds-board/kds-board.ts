import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { LucideAngularModule, Rows3, History, Funnel } from 'lucide-angular';
import { SignalrService } from '../../../../core/services/signalr-service';
import { KdsService } from '../../../../core/services/kds-service';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { KdsStatus } from '../../../../core/enums/kds-status';
import { OrderStatus } from '../../../../core/enums/order-status';
import { Subscription } from 'rxjs/internal/Subscription';
import { forkJoin } from 'rxjs';
import { OrderItemModifierResponseDto } from '../../../../core/models/modifier.model';

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
  private destroyRef = inject(DestroyRef);

  private timerInterval: any;

  private notificationSound = new Audio('/notificationSound.mp3');
  private cancelSound = new Audio('/cancelSound.wav');

  activeOrders: OrderResponseDto[] = [];

  public readonly KdsStatus = KdsStatus;
  public readonly OrderStatus = OrderStatus;

  private subscriptions = new Subscription();

  ngOnInit(): void {
    this.loadPendingItems();
    this.signalrService.startConnection();

    this.timerInterval = setInterval(() => {}, 60000);

    this.destroyRef.onDestroy(() => {
      this.signalrService.stopConnection();
    });

    this.subscriptions.add(
      this.signalrService.orderCanceled$
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((canceledOrder) => {
          this.handleOrderCanceled(canceledOrder);

          this.playCancelSound();
        }),
    );

    this.subscriptions.add(
      this.signalrService.orderUpdated$
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((updatedOrder) => {
          this.handleOrderUpdate(updatedOrder);
        }),
    );

    this.subscriptions.add(
      this.signalrService.orderDelivered$
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((deliveredOrder) => {
          this.removeOrderFromScreen(deliveredOrder.id);
        }),
    );
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
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

  handleOrderCanceled(canceledOrder: OrderResponseDto): void {
    const orderToCancel = this.activeOrders.find((o) => o.id === canceledOrder.id);

    if (orderToCancel) {
      orderToCancel.orderStatus = OrderStatus.Canceled as any;

      setTimeout(() => {
        this.activeOrders = this.activeOrders.filter((o) => o.id !== canceledOrder.id);
      }, 10000);
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

  private playCancelSound(): void {
    this.cancelSound.currentTime = 0;

    this.cancelSound.play().catch((error) => {
      console.warn(
        'O navegador bloqueou o áudio automático. O usuário precisa clicar na tela primeiro.',
        error,
      );
    });
  }

  getGroupedModifiers(modifiers: OrderItemModifierResponseDto[]) {
    if (!modifiers || modifiers.length === 0) return [];

    const groups: { [key: string]: OrderItemModifierResponseDto[] } = {};

    modifiers.forEach((mod) => {
      const groupName = mod.groupName || 'Adicionais';
      if (!groups[groupName]) {
        groups[groupName] = [];
      }
      groups[groupName].push(mod);
    });

    return Object.keys(groups).map((key) => ({
      groupName: key,
      options: groups[key],
    }));
  }

  getHeaderColorClass(createdAt: string | Date): string {
    const orderDate = new Date(createdAt);
    const now = new Date();

    const diffInMs = now.getTime() - orderDate.getTime();
    const diffInMinutes = Math.floor(diffInMs / 60000);

    if (diffInMinutes >= 30) {
      return 'bg-red'; 
    } else if (diffInMinutes >= 15) {
      return 'bg-orange'; 
    } else {
      return 'bg-green';
    }
  }
}
