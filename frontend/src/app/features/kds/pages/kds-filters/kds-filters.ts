import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, forkJoin } from 'rxjs';
import { SignalrService } from '../../../../core/services/signalr-service';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { KdsService } from '../../../../core/services/kds-service';
import { CategoryService } from '../../../../core/services/category-service';
import { CategoryResponseDto } from '../../../../core/models/category.model';
import { KdsStatus } from '../../../../core/enums/kds-status';
import { OrderStatus } from '../../../../core/enums/order-status';
import { LucideAngularModule, Search, ListFilter } from 'lucide-angular';

@Component({
  selector: 'app-kds-filters',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './kds-filters.html',
  styleUrl: './kds-filters.scss',
})
export class KdsFilters implements OnInit, OnDestroy {
  readonly Search = Search;
  readonly ListFilter = ListFilter;

  private kdsService = inject(KdsService);
  private signalrService = inject(SignalrService);
  private categoryService = inject(CategoryService);

  private notificationSound = new Audio('/notificationSound.mp3');

  activeOrders: OrderResponseDto[] = [];
  categories: CategoryResponseDto[] = [];

  selectedCategoryIds = new Set<string>();
  isCategoryDropdownOpen = false;

  public readonly KdsStatus = KdsStatus;
  public readonly OrderStatus = OrderStatus;

  private subscriptions = new Subscription();

  ngOnInit(): void {
    this.loadCategories();
    this.loadPendingOrders();
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
    this.subscriptions.unsubscribe();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Erro ao carregar categorias', err),
    });
  }

  loadPendingOrders(): void {
    this.kdsService.getPendingKdsOrders().subscribe({
      next: (orders) => {
        this.activeOrders = orders;
      },
      error: (err) => console.error('Erro ao carregar comandas no filtro', err),
    });
  }

  get filteredOrders(): OrderResponseDto[] {
    return this.activeOrders
      .map((order) => {
        const matchingItems =
          this.selectedCategoryIds.size === 0
            ? order.items
            : order.items.filter((item) => this.selectedCategoryIds.has(item.categoryId));

        if (matchingItems.length === 0) return null;

        return { ...order, items: matchingItems };
      })
      .filter((order) => order !== null) as OrderResponseDto[];
  }

  markItemAsDone(orderId: string, itemId: string): void {
    const order = this.activeOrders.find((o) => o.id === orderId);
    if (order) {
      const item = order.items.find((i) => i.id === itemId);
      if (item) item.kdsStatus = KdsStatus.Done;
    }

    this.kdsService.updateItemStatus(orderId, itemId, KdsStatus.Done).subscribe({
      next: () => {},
      error: (err) => console.error('Erro ao atualizar item na cozinha:', err),
    });
  }

  markOrderAsDone(order: OrderResponseDto): void {
    const pendingItems = order.items.filter((i) => i.kdsStatus !== KdsStatus.Done);
    if (pendingItems.length === 0) return;

    const requests = pendingItems.map((item) =>
      this.kdsService.updateItemStatus(order.id, item.id, KdsStatus.Done),
    );

    forkJoin(requests).subscribe({
      next: () => console.log(`Itens da comanda #${order.id.substring(0, 6)} finalizados!`),
      error: (err) => {
        console.error('Erro ao finalizar comanda na API:', err);
        this.loadPendingOrders();
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
        const hasMatchingItem =
          this.selectedCategoryIds.size === 0 ||
          updatedOrder.items.some((item) => this.selectedCategoryIds.has(item.categoryId));

        if (hasMatchingItem) {
          this.playSound();
        }
      }
      this.activeOrders.sort(
        (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
      );
    }
  }

  removeOrderFromScreen(orderId: string): void {
    this.activeOrders = this.activeOrders.filter((o) => o.id !== orderId);
  }

  selectAllCategories() {
    this.selectedCategoryIds.clear();
  }

  toggleCategory(categoryId: string) {
    if (this.selectedCategoryIds.has(categoryId)) {
      this.selectedCategoryIds.delete(categoryId);
    } else {
      this.selectedCategoryIds.add(categoryId);
    }
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
