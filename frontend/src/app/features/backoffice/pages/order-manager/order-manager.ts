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
import {
  OrderStatusClassPipe,
  OrderStatusLabelPipe,
} from '../../../../core/pipes/order-status-pipe-pipe';
import {
  PaymentStatusClassPipe,
  PaymentStatusLabelPipe,
} from '../../../../core/pipes/payment-status-pipe-pipe';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ToastrService } from 'ngx-toastr';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GroupByPipe } from '../../../../core/pipes/group-by-pipe';

@Component({
  selector: 'app-order-manager',
  imports: [
    CommonModule,
    FormsModule,
    InputComponent,
    LucideAngularModule,
    OrderStatusClassPipe,
    OrderStatusLabelPipe,
    PaymentStatusClassPipe,
    PaymentStatusLabelPipe,
    Pagination,
    ModalComponent,
    GroupByPipe,
    ButtonComponent,
  ],
  templateUrl: './order-manager.html',
  styleUrl: './order-manager.scss',
})
export class OrderManager extends BaseCrud<OrderRequestDto, OrderResponseDto, string> {
  readonly TextSearch = TextSearch;

  readonly OrderStatus = OrderStatus;

  searchSubject = new Subject<string>();

  private orderService = inject(OrderService);
  private toastr = inject(ToastrService);

  constructor() {
    super(inject(ChangeDetectorRef));
    this.searchSubject
      .pipe(debounceTime(500), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm = term;
        this.pageNumber = 1;
        this.loadData();
      });
  }

  get formattedOrderId(): string {
    if (!this.currentItem || !this.currentItem.id) return '';
    return String(this.currentItem.id).slice(0, 6).toUpperCase();
  }

  isOrderBy(sortBy: string): boolean {
    return this.currentSortColumn === sortBy;
  }

  descendingIcon(): string {
    return this.isDescending ? '↓' : '↑';
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
          this.toastr.success('Pedido cancelado com sucesso!');
          this.closeModal();
          this.loadData();
        },
        error: (err) => {
          this.toastr.error('Erro ao cancelar o pedido.');
          console.error('Erro ao cancelar o pedido:', err);
        },
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
