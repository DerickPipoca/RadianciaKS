import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnDestroy, OnInit } from '@angular/core';
import { SignalrService } from '../../../core/services/signalr-service';
import { KdsOrderGroup, OrderItemResponseDto } from '../../../core/models/order.model';
import { KdsService } from '../../../core/services/kds-service';
import { KdsStatus } from '../../../core/enums/kds-status';
import { firstValueFrom, Subscription } from 'rxjs';
import { LucideAngularModule, Rows3, History, Funnel } from 'lucide-angular';

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

  pendingItems: OrderItemResponseDto[] = [];
  KdsStatus = KdsStatus;

  checkedItems = new Set<string>();

  private subscriptions = new Subscription();

  ngOnInit(): void {
    this.loadPendingItems();
    this.signalrService.startConnection();

    this.subscriptions.add(
      this.signalrService.newItem$.subscribe((newItem) => {
        if (!this.pendingItems.find((i) => i.id === newItem.id)) {
          this.pendingItems = [...this.pendingItems, newItem];
          this.playSound();
        }
      }),
    );

    this.subscriptions.add(
      this.signalrService.itemReady$.subscribe((readyItem) => {
        this.pendingItems = this.pendingItems.filter((i) => i.id !== readyItem.id);
      }),
    );
  }

  ngOnDestroy(): void {
    this.signalrService.stopConnection();
    this.subscriptions.unsubscribe();
  }

  loadPendingItems(): void {
    this.kdsService.getPendingItems().subscribe({
      next: (items) => (this.pendingItems = items),
      error: (err) => console.error('Erro ao carregar KDS:', err),
    });
  }

  get groupedOrders(): KdsOrderGroup[] {
    const map = new Map<string, KdsOrderGroup>();

    for (const item of this.pendingItems) {
      if (!map.has(item.orderId)) {
        const orderTime = item.createdAt ? new Date(item.createdAt) : new Date();

        map.set(item.orderId, {
          orderId: item.orderId,
          items: [],
          status: item.kdsStatus,
          tableNumber: '00',
          customerName: 'Cliente',
          time: orderTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          createdAt: orderTime,
        });
      }
      map.get(item.orderId)!.items.push(item);
    }

    const groups = Array.from(map.values());

    groups.sort((a, b) => a.createdAt.getTime() - b.createdAt.getTime());

    return groups;
  }

  toggleItemCheck(itemId: string) {
    if (this.checkedItems.has(itemId)) {
      this.checkedItems.delete(itemId);
    } else {
      this.checkedItems.add(itemId);
    }
  }

  isItemChecked(itemId: string): boolean {
    return this.checkedItems.has(itemId);
  }

  async markOrderAsDone(orderGroup: KdsOrderGroup) {
    for (const item of orderGroup.items) {
      try {
        await firstValueFrom(
          this.kdsService.updateItemStatus(orderGroup.orderId, item.id, KdsStatus.Done),
        );

        this.pendingItems = this.pendingItems.filter((i) => i.id !== item.id);
        this.checkedItems.delete(item.id);
      } catch (err) {
        console.error('Erro ao atualizar item', item.id, err);
      }
    }
  }

  updateStatus(item: OrderItemResponseDto, newStatus: KdsStatus): void {
    const orderId = (item as any).orderId;

    if (!orderId) {
      alert('Erro: ID do Pedido não encontrado neste item.');
      return;
    }

    this.kdsService.updateItemStatus(orderId, item.id, newStatus).subscribe({
      next: () => {
        if (newStatus === KdsStatus.Done) {
          this.pendingItems = this.pendingItems.filter((i) => i.id !== item.id);
        } else {
          item.kdsStatus = newStatus;
        }
      },
      error: () => alert('Erro ao atualizar o status na cozinha.'),
    });
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
