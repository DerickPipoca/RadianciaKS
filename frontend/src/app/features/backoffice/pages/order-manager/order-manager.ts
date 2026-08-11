import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { OrderRequestDto, OrderResponseDto } from '../../../../core/models/order.model';
import { OrderService } from '../../../../core/services/order-service';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { LucideAngularModule, TextSearch } from 'lucide-angular';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderStatus } from '../../../../core/enums/order-status';
import { PaymentStatus } from '../../../../core/enums/payment-status';

@Component({
  selector: 'app-order-manager',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent, LucideAngularModule],
  templateUrl: './order-manager.html',
  styleUrl: './order-manager.scss',
})
export class OrderManager extends BaseCrud<OrderRequestDto, OrderResponseDto, string> {
  readonly TextSearch = TextSearch;

  searchSubject = new Subject<string>();

  private orderService = inject(OrderService);

  pageNumber: number = 1;
  pageSize: number = 12;
  totalRecords: number = 0;
  searchTerm: string = '';
  sortBy: string = '';
  descendingSort: boolean = false;

  constructor() {
    super(inject(ChangeDetectorRef));
    this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
      this.pageNumber = 1;
      this.loadData();
    });
  }

  get formattedOrderId(): string {
    if (!this.currentItem || !this.currentItem.id) return '';
    return String(this.currentItem.id).slice(0, 6).toUpperCase();
  }

  override loadData(): void {
    this.orderService
      .getAll({
        pageNumber: this.pageNumber,
        pageSize: this.pageSize,
        searchTerm: this.searchTerm,
        sortBy: this.sortBy,
        isDescending: this.descendingSort,
      })
      .subscribe({
        next: (response) => {
          this.dataList = response.data;
          this.totalRecords = response.totalRecords;
        },
        error: (err) => console.error('Erro ao carregar:', err),
      });
  }

  orderBy(sortBy: string): void {
    if (this.sortBy !== sortBy) {
      this.sortBy = sortBy;
      this.descendingSort = false;
    } else {
      this.descendingSort = !this.descendingSort;
    }
    this.pageNumber = 1;
    this.loadData();
  }

  isOrderBy(sortBy: string): boolean {
    return this.sortBy === sortBy;
  }

  descendingIcon(): string {
    return this.descendingSort ? '↓' : '↑';
  }

  changePage(newPage: number): void {
    if (newPage < 1 || newPage > this.getTotalPages()) return;
    this.pageNumber = newPage;
    this.loadData();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalRecords / this.pageSize) || 1;
  }

  get pageNumbers(): number[] {
    const total = this.getTotalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  get rangeStart(): number {
    return this.totalRecords === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    const end = this.pageNumber * this.pageSize;
    return end > this.totalRecords ? this.totalRecords : end;
  }

  getOrderStatusLabel(status: OrderStatus): string {
    switch (status) {
      case OrderStatus.Canceled:
        return 'Cancelado';
      case OrderStatus.Preparing:
        return 'Preparando';
      case OrderStatus.ReadyToServe:
        return 'Pronto p/ servir';
      case OrderStatus.Delivered:
        return 'Entregue';
      case OrderStatus.Open:
        return 'Aberto';
      default:
        return 'Desconhecido';
    }
  }

  getPaymentStatusLabel(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Pending:
        return 'Pendente';
      case PaymentStatus.Partial:
        return 'Pago Parcial';
      case PaymentStatus.Paid:
        return 'Pago';
      case PaymentStatus.Refunded:
        return 'Estornado';
      default:
        return 'Desconhecido';
    }
  }

  getPaymentStatusClass(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Pending:
        return 'pending';
      case PaymentStatus.Partial:
        return 'partial';
      case PaymentStatus.Paid:
        return 'paid';
      case PaymentStatus.Refunded:
        return 'refunded';
      default:
        return 'pending';
    }
  }

  getOrderStatusClass(status: OrderStatus): string {
    switch (status) {
      case OrderStatus.Canceled:
        return 'canceled';
      case OrderStatus.Preparing:
        return 'preparing';
      case OrderStatus.ReadyToServe:
        return 'ready-to-serve';
      case OrderStatus.Delivered:
        return 'delivered';
      case OrderStatus.Open:
        return 'open';
      default:
        return 'open';
    }
  }

  protected override get crudService(): any {
    return this.orderService;
  }

  protected override getEmptyItem(): OrderResponseDto {
    return {} as OrderResponseDto;
  }

  protected override getItemId(item: OrderResponseDto): string {
    return item.id;
  }

  cancel(id: string) {
    if (confirm('Tem certeza que deseja cancelar este pedido?')) {
      this.orderService.cancelOrder(id).subscribe({
        next: () => {
          alert('Pedido cancelado com sucesso!');
          this.closeModal();
          this.loadData();
        },
        error: (err) => console.error('Erro ao cancelar o pedido:', err),
      });
    }
  }

  protected override mapToRequest(item: OrderResponseDto): any {
    return item;
  }

  protected override validateSave(item: OrderRequestDto): boolean {
    return true;
  }
}
