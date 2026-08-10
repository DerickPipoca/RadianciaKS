import { OrderResponseDto } from './../../../core/models/order.model';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { OrderService } from '../../../core/services/order-service';
import { OrderStatus } from '../../../core/enums/order-status';
import { SignalrService } from '../../../core/services/signalr-service';
import { debounceTime, merge, Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  private signalrService = inject(SignalrService);
  private subscriptions = new Subscription();

  readyOrders: OrderResponseDto[] = [];

  ngOnInit(): void {
    this.loadReadyOrders();
    this.signalrService.startConnection();

    this.subscriptions.add(
      merge(this.signalrService.orderUpdated$, this.signalrService.orderDelivered$)
        .pipe(debounceTime(300))
        .subscribe(() => {
          this.loadReadyOrders();
        }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadReadyOrders(): void {
    let params = {
      pageNumber: 1,
      pageSize: 250,
      sortBy: 'createdat',
      status: OrderStatus.ReadyToServe,
    };
    this.orderService.getAll(params).subscribe({
      next: (response) => {
        this.readyOrders = response.data;
        console.log(response.data);
      },
      error: (err) => console.error('Erro ao buscar pedidos na API:', err),
    });
  }
}
