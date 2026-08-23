import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, RefreshCcw } from 'lucide-angular';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { OrderStatus } from '../../../../core/enums/order-status';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { OrderService } from '../../../../core/services/order-service';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import {
  OrderStatusClassPipe,
  OrderStatusLabelPipe,
} from '../../../../core/pipes/order-status-pipe-pipe';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrderItemModifierResponseDto } from '../../../../core/models/modifier.model';

@Component({
  selector: 'app-kds-history',
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    InputComponent,
    OrderStatusClassPipe,
    OrderStatusLabelPipe,
    Pagination,
  ],
  templateUrl: './kds-history.html',
  styleUrl: './kds-history.scss',
})
export class KdsHistory implements OnInit {
  readonly Search = Search;
  readonly RefreshCcw = RefreshCcw;
  private orderService = inject(OrderService);

  historicalOrders: OrderResponseDto[] = [];
  public readonly OrderStatus = OrderStatus;

  searchTerm: string = '';
  searchSubject = new Subject<string>();

  pageNumber = 1;
  pageSize = 12;
  totalRecords = 0;

  constructor() {
    this.searchSubject
      .pipe(debounceTime(500), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm = term;
        this.pageNumber = 1;
        this.loadHistory();
      });
  }

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.orderService
      .getAll({
        pageNumber: this.pageNumber,
        pageSize: this.pageSize,
        searchTerm: this.searchTerm,
        sortBy: 'createdAt',
        isDescending: true,
      })
      .subscribe({
        next: (res) => {
          this.historicalOrders = res.data;
          this.totalRecords = res.totalRecords;
        },
        error: (err) => console.error('Erro ao carregar histórico do KDS', err),
      });
  }

  changePage(newPage: number) {
    this.pageNumber = newPage;
    this.loadHistory();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalRecords / this.pageSize);
  }

  getOrderStatusLabel(order: OrderResponseDto): string {
    switch (order.orderStatus) {
      case OrderStatus.Canceled:
        return 'Cancelado';
      case OrderStatus.Preparing:
        return 'Preparando';
      case OrderStatus.ReadyToServe:
        return 'Pronto';
      case OrderStatus.Delivered:
        return 'Entregue';
      case OrderStatus.Open:
        return 'Aberto';
      default:
        return 'Desconhecido';
    }
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
}
