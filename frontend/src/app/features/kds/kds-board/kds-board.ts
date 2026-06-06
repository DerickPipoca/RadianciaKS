import { CommonModule } from '@angular/common';
import { Component, effect, inject, OnInit } from '@angular/core';
import { SignalrService } from '../../../core/services/signalr-service';
import { OrderItemResponseDto } from '../../../core/models/order.model';

@Component({
  selector: 'app-kds-board',
  imports: [CommonModule],
  templateUrl: './kds-board.html',
  styleUrl: './kds-board.scss',
})
export class KdsBoard implements OnInit {
  private signalRService = inject(SignalrService);

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
    this.moveItemToReadyLocal(item);
  }

  ngOnInit(): void {
    throw new Error('Method not implemented.');
  }

  private moveItemToReadyLocal(item: OrderItemResponseDto): void {
    this.pendingItems = this.pendingItems.filter((i) => i.id !== item.id);
    if (!this.readyItems.find((i) => i.id === item.id)) {
      this.readyItems.unshift(item);
    }
  }
}
