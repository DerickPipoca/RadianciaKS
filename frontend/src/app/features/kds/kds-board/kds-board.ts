import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnDestroy, OnInit } from '@angular/core';
import { SignalrService } from '../../../core/services/signalr-service';
import { OrderItemResponseDto } from '../../../core/models/order.model';
import { KdsService } from '../../../core/services/kds-service';
import { KdsStatus } from '../../../core/enums/kds-status';

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
