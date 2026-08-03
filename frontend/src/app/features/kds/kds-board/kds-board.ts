import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnDestroy, OnInit } from '@angular/core';
import { SignalrService } from '../../../core/services/signalr-service';
import { KdsOrderGroup, OrderItemResponseDto } from '../../../core/models/order.model';
import { KdsService } from '../../../core/services/kds-service';
import { KdsStatus } from '../../../core/enums/kds-status';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-kds-board',
  imports: [CommonModule],
  templateUrl: './kds-board.html',
  styleUrl: './kds-board.scss',
})
export class KdsBoard implements OnInit, OnDestroy {
  private kdsService = inject(KdsService);
  private signalrService = inject(SignalrService);

  pendingItems: OrderItemResponseDto[] = [];
  KdsStatus = KdsStatus;

  checkedItems = new Set<string>();

  constructor() {
    effect(() => {
      const newItem = this.signalrService.newItemSignal();
      if (newItem) {
        if (!this.pendingItems.find((i) => i.id === newItem.id)) {
          this.pendingItems.push(newItem);
        }
      }
    });

    effect(() => {
      const readyItem = this.signalrService.itemReadySignal();
      if (readyItem) {
        this.pendingItems = this.pendingItems.filter((i) => i.id !== readyItem.id);
      }
    });
  }

  ngOnInit(): void {
    this.loadPendingItems();
    this.signalrService.startConnection();
  }

  ngOnDestroy(): void {
    this.signalrService.stopConnection();
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
        });
      }
      map.get(item.orderId)!.items.push(item);
    }

    return Array.from(map.values());
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
}
