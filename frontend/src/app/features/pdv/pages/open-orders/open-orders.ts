import { OrderResponseDto } from './../../../../core/models/order.model';
import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  inject,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule, Search, SquarePen, Plus } from 'lucide-angular';
import { OrderService } from '../../../../core/services/order-service';
import { CartService } from '../../../../core/services/cart-service';
import { OrderStatus } from '../../../../core/enums/order-status';
import { debounceTime, distinctUntilChanged, Subject, Subscription } from 'rxjs';
import { PaymentStatus } from '../../../../core/enums/payment-status';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import {
  OrderStatusClassPipe,
  OrderStatusLabelPipe,
} from '../../../../core/pipes/order-status-pipe-pipe';
import {
  PaymentStatusLabelPipe,
  PaymentStatusClassPipe,
} from '../../../../core/pipes/payment-status-pipe-pipe';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ClickOutsideDirective } from '../../../../core/directives/click-outside-directive';

@Component({
  selector: 'app-open-orders',
  imports: [
    CommonModule,
    LucideAngularModule,
    InputComponent,
    OrderStatusClassPipe,
    OrderStatusLabelPipe,
    PaymentStatusLabelPipe,
    PaymentStatusClassPipe,
    ClickOutsideDirective,
    ModalComponent,
  ],
  templateUrl: './open-orders.html',
  styleUrl: './open-orders.scss',
})
export class OpenOrders implements OnInit, OnDestroy {
  readonly Search = Search;
  readonly SquarePen = SquarePen;
  readonly Plus = Plus;

  PaymentStatus = PaymentStatus;

  searchTerm: string = '';
  searchSubject = new Subject<string>();
  private subscription = new Subscription();

  currentSortColumn: string = 'createdAt';
  isDescending: boolean = true;

  private orderService = inject(OrderService);
  private cartService = inject(CartService);
  private router = inject(Router);

  public readonly OrderStatus = OrderStatus;

  dropdownOpen = false;
  filterStatus: string = '';
  availableStatuses = ['Open', 'ReadyToServe', 'Delivered', 'Paid'];

  private allOrders: OrderResponseDto[] = [];
  selectedOrder: OrderResponseDto | null = null;

  constructor() {
    this.subscription.add(
      this.searchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {}),
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    let statusEnum: OrderStatus | undefined = undefined;
    let paymentStatus: PaymentStatus | undefined = undefined;
    if (this.filterStatus === 'Paid') {
      paymentStatus = PaymentStatus.Paid;
    } else if (this.filterStatus) {
      statusEnum = this.filterStatus ? (OrderStatus[this.filterStatus as any] as any) : null;
    }

    this.orderService
      .getAll({
        pageNumber: 1,
        pageSize: 200,
        sortBy: this.currentSortColumn,
        isDescending: this.isDescending,
        status: statusEnum,
        paymentStatus: paymentStatus,
      })
      .subscribe({
        next: (res) => {
          this.allOrders = res.data;
        },
        error: (err) => console.error('Erro ao carregar pedidos', err),
      });
  }

  get activeOrders(): OrderResponseDto[] {
    return this.allOrders.filter((order) => {
      if (order.orderStatus === OrderStatus.Canceled) {
        return false;
      }

      if (
        order.paymentStatus === PaymentStatus.Paid &&
        order.orderStatus === OrderStatus.Delivered
      ) {
        return false;
      }
      return true;
    });
  }

  canCheckout(order: OrderResponseDto): boolean {
    return order.paymentStatus !== PaymentStatus.Paid;
  }

  openTableForEditing(order: OrderResponseDto) {
    this.cartService.loadOrderForEditing(order);
    this.router.navigate(['/pdv/catalogo']);
  }

  goBack(): void {
    this.router.navigate(['/pdv/home']);
  }

  openDetails(order: OrderResponseDto): void {
    this.selectedOrder = order;
  }

  closeDetails(): void {
    this.selectedOrder = null;
  }

  goToCheckout(order: OrderResponseDto): void {
    this.router.navigate(['/pdv/checkout'], { queryParams: { orderId: order.id, abertos: true } });
  }

  changeSort(column: string): void {
    if (this.currentSortColumn === column) {
      this.isDescending = !this.isDescending;
    } else {
      this.currentSortColumn = column;
      this.isDescending = true;
    }
  }

  get filteredOrders(): OrderResponseDto[] {
    let filtered = this.activeOrders;

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(
        (o) =>
          (o.tableNumber && o.tableNumber.toLowerCase().includes(term)) ||
          o.id.toLowerCase().includes(term),
      );
    }

    return filtered.sort((a, b) => {
      let valA: any = a[this.currentSortColumn as keyof OrderResponseDto];
      let valB: any = b[this.currentSortColumn as keyof OrderResponseDto];

      if (this.currentSortColumn === 'createdAt') {
        valA = new Date(valA).getTime();
        valB = new Date(valB).getTime();
      }

      if (valA < valB) return this.isDescending ? 1 : -1;
      if (valA > valB) return this.isDescending ? -1 : 1;
      return 0;
    });
  }

  formattedOrderId(order: OrderResponseDto): string {
    if (!order || !order.id) return '';
    return String(order.id).slice(0, 6).toUpperCase();
  }

  changeFilterStatus(status: string) {
    this.filterStatus = status;
    this.onStatusChange();
  }

  onStatusChange() {
    this.searchTerm = '';
    this.loadOrders();
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }
}
