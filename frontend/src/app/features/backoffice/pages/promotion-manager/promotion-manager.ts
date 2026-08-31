import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { Flame, LucideAngularModule, Pause, Play } from 'lucide-angular';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ClickOutsideDirective } from '../../../../core/directives/click-outside-directive';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { PromotionRequestDto, PromotionResponseDto } from '../../../../core/models/promotion.model';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';
import { TextSearch } from 'lucide-angular/src/icons';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { PromotionService } from '../../../../core/services/promotion-service';
import { ProductService } from '../../../../core/services/product-service';
import { ToastrService } from 'ngx-toastr';
import { ProductResponseDto } from '../../../../core/models/product.model';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-promotion-manager',
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    InputComponent,
    LucideAngularModule,
    Pagination,
    ModalComponent,
    ClickOutsideDirective,
  ],
  templateUrl: './promotion-manager.html',
  styleUrl: './promotion-manager.scss',
})
export class PromotionManager
  extends BaseCrud<PromotionRequestDto, PromotionResponseDto, string>
  implements OnInit
{
  readonly TextSearch = TextSearch;
  readonly Flame = Flame;
  readonly Play = Play;
  readonly Pause = Pause;

  searchSubject = new Subject<string>();

  private promotionService = inject(PromotionService);
  private productService = inject(ProductService);
  private toastr = inject(ToastrService);

  products: ProductResponseDto[] = [];
  selectedBaseProduct: ProductResponseDto | null = null;
  dropdownOpen = false;

  modifierOverrides: { [optionId: string]: number | null } = {};

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

  protected override get crudService(): ICrudService<
    PromotionRequestDto,
    PromotionResponseDto,
    string
  > {
    return this.promotionService as any;
  }

  protected override getEmptyItem(): PromotionRequestDto {
    return {
      name: '',
      description: '',
      baseProductId: '',
      promotionalPrice: null,
      promotionModifiers: [],
    };
  }

  override ngOnInit(): void {
    this.loadProducts();
    super.ngOnInit();
  }

  loadProducts(): void {
    this.productService.getAll({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (response: any) => {
        this.products = Array.isArray(response) ? response : response.data || [];
      },
      error: (err) => console.error('Erro ao carregar produtos', err),
    });
  }

  protected override getItemId(item: PromotionResponseDto): string {
    return item.id;
  }

  protected override mapToRequest(item: PromotionResponseDto): PromotionRequestDto {
    this.loadBaseProductDetails(item.baseProduct.id);

    this.modifierOverrides = {};

    if (item.baseProduct && item.baseProduct.modifierGroups) {
      item.baseProduct.modifierGroups.forEach((group) => {
        group.options.forEach((opt) => {
          if (opt.isPromotional && opt.promotionalPrice !== undefined) {
            this.modifierOverrides[opt.id] = opt.promotionalPrice;
          }
        });
      });
    }

    return {
      name: item.name,
      description: item.description,
      baseProductId: item.baseProduct.id,
      promotionalPrice: item.baseProduct.isPromotional
        ? (item.baseProduct.promotionalPrice ?? null)
        : null,
      promotionModifiers: [],
    };
  }

  protected override validateSave(item: PromotionRequestDto): boolean {
    if (!item.name || item.name.trim() === '') {
      this.toastr.warning('O nome da promoção é obrigatório.');
      return false;
    }
    if (!item.baseProductId) {
      this.toastr.warning('Selecione o produto base que receberá o desconto.');
      return false;
    }

    item.promotionModifiers = [];
    for (const [optionId, price] of Object.entries(this.modifierOverrides)) {
      if (price !== null && price >= 0) {
        item.promotionModifiers.push({
          modifierOptionId: optionId,
          overridePrice: Number(price),
        });
      }
    }

    return true;
  }

  override closeModal(): void {
    super.closeModal();
    this.dropdownOpen = false;
    this.selectedBaseProduct = null;
    this.modifierOverrides = {};
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  selectProduct(productId: string) {
    this.currentItem.baseProductId = productId;
    this.dropdownOpen = false;
    this.loadBaseProductDetails(productId);
  }

  loadBaseProductDetails(productId: string) {
    this.productService.getById(productId).subscribe({
      next: (product) => {
        this.selectedBaseProduct = product;
      },
    });
  }

  getSelectedProductName(): string {
    const selected = this.products?.find((p) => p.id === this.currentItem.baseProductId);
    return selected ? selected.name : '';
  }

  descendingIcon(): string {
    return this.isDescending ? '↓' : '↑';
  }

  toggleRunningStatus(promo: PromotionResponseDto) {
    this.promotionService.toggleRunningStatus(promo.id).subscribe({
      next: (isRunning: boolean) => {
        this.toastr.success(`Promoção ${isRunning ? 'ativada' : 'pausada'}!`);
        this.loadData();
      },
    });
  }
}
