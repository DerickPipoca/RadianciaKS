import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { CategoryService } from '../../../../core/services/category-service';
import { ProductService } from '../../../../core/services/product-service';
import { CategoryResponseDto } from '../../../../core/models/category.model';
import { ProductResponseDto } from '../../../../core/models/product.model';
import { CartService } from '../../../../core/services/cart-service';
import { CommonModule } from '@angular/common';
import { ModifierModal } from '../../components/modifier-modal/modifier-modal';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { LucideAngularModule, TextSearch, Utensils, Flame } from 'lucide-angular';
import { debounceTime, distinctUntilChanged, Subject, Subscription } from 'rxjs';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { PromotionResponseDto } from '../../../../core/models/promotion.model';
import { PromotionService } from '../../../../core/services/promotion-service';

@Component({
  selector: 'app-catalog',
  imports: [
    CommonModule,
    ModifierModal,
    RouterLink,
    FormsModule,
    InputComponent,
    LucideAngularModule,
    Pagination,
  ],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
})
export class Catalog implements OnInit, OnDestroy {
  readonly TextSearch = TextSearch;
  readonly Utensils = Utensils;
  readonly Flame = Flame;

  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private promotionService = inject(PromotionService);

  categories: CategoryResponseDto[] = [];
  products: ProductResponseDto[] = [];
  filteredProducts: ProductResponseDto[] = [];

  promotions: PromotionResponseDto[] = [];
  viewingPromotions: boolean = false;

  searchTerm: string = '';
  pageNumber: number = 1;
  pageSize: number = 12;
  totalRecords: number = 0;

  searchSubject = new Subject<string>();
  private subscription = new Subscription();

  activeCategory: CategoryResponseDto | null = null;
  selectedProductForModal: ProductResponseDto | null = null;
  selectedPromotionIdForModal: string | null = null;

  constructor() {
    this.subscription.add(
      this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
        this.pageNumber = 1;
        this.fetchProducts();
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadPromotions();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Erro ao carregar categorias', err),
    });
  }

  loadPromotions() {
    this.promotionService.getActivePromotions().subscribe({
      next: (data) => (this.promotions = data),
      error: (err) => console.error(err),
    });
  }

  showPromotions() {
    this.viewingPromotions = true;
  }

  fetchProducts(): void {
    if (!this.activeCategory) return;

    this.productService
      .getAll({
        pageNumber: this.pageNumber,
        pageSize: this.pageSize,
        searchTerm: this.searchTerm,
        categoryId: this.activeCategory?.id,
      })
      .subscribe({
        next: (products) => {
          this.products = products.data;
          this.filteredProducts = [...this.products];
          this.totalRecords = products.totalRecords;
        },
        error: (err) => console.error('Erro ao carregar produtos', err),
      });
  }

  selectCategory(category: CategoryResponseDto): void {
    this.viewingPromotions = false;
    this.activeCategory = category;
    this.pageNumber = 1;
    this.searchTerm = '';
    this.fetchProducts();
  }

  clearCategory(): void {
    this.activeCategory = null;
    this.viewingPromotions = false;
    this.pageNumber = 1;
    this.searchTerm = '';
  }

  changePage(newPage: number): void {
    this.pageNumber = newPage;
    this.fetchProducts();
  }

  onProductClick(product: ProductResponseDto): void {
    this.selectedProductForModal = product;
    this.selectedPromotionIdForModal = null;
  }

  openPromotionModal(promotion: PromotionResponseDto) {
    this.selectedPromotionIdForModal = promotion.id; 
    this.selectedProductForModal = promotion.baseProduct;
  }

  closeModal() {
    this.selectedProductForModal = null;
    this.selectedPromotionIdForModal = null;
  }
}
