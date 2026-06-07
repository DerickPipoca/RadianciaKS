import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnInit } from '@angular/core';
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
export class KdsBoard implements OnInit {
  private signalRService = inject(SignalrService);
  private kdsService = inject(KdsService);

  pendingItems: OrderItemResponseDto[] = [];
  readyItems: OrderItemResponseDto[] = [];

  constructor() {
    effect(() => {
      const newItem = this.signalRService.newItemSignal();
      if (newItem) {
        if (!this.pendingItems.find((i) => i.id === newItem.id)) {
          this.pendingItems.push(newItem);
        }
      }
    });

    effect(() => {
      const readyItem = this.signalRService.itemReadySignal();
      if (readyItem) {
        this.moveItemToReadyLocal(readyItem);
      }
    });
  }
  markAsReady(item: OrderItemResponseDto): void {
    if (!item.orderId || !item.id) return;

    const kdsStatus = KdsStatus.Done;

    this.kdsService.updateItemStatus(item.orderId, item.id, kdsStatus).subscribe({
      next: () => {
        this.moveItemToReadyLocal(item);
      },
      error: (err) => console.error('Erro ao atualizar status na API:', err),
    });
  }

  ngOnInit(): void {
    this.kdsService.getPendingItems().subscribe({
      next: (items) => {
        this.pendingItems = items;
      },
      error: (err) => console.error('Erro ao carregar fila da cozinha:', err),
    });
  }

  private moveItemToReadyLocal(item: OrderItemResponseDto): void {
    this.pendingItems = this.pendingItems.filter((i) => i.id !== item.id);
    if (!this.readyItems.find((i) => i.id === item.id)) {
      this.readyItems.unshift(item);
    }
  }
}
