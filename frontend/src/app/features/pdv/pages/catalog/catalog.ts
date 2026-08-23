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
import { LucideAngularModule, TextSearch, Utensils } from 'lucide-angular';
import { debounceTime, distinctUntilChanged, Subject, Subscription } from 'rxjs';
import { Pagination } from '../../../../shared/components/pagination/pagination';

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

  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  categories: CategoryResponseDto[] = [];
  products: ProductResponseDto[] = [];
  filteredProducts: ProductResponseDto[] = [];

  searchTerm: string = '';
  pageNumber: number = 1;
  pageSize: number = 12;
  totalRecords: number = 0;

  searchSubject = new Subject<string>();
  private subscription = new Subscription();

  activeCategory: CategoryResponseDto | null = null;
  selectedProductForModal: ProductResponseDto | null = null;

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
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Erro ao carregar categorias', err),
    });
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
    this.activeCategory = category;
    this.pageNumber = 1;
    this.searchTerm = '';
    this.fetchProducts();
  }

  clearCategory(): void {
    this.activeCategory = null;
    this.pageNumber = 1;
    this.searchTerm = '';
  }

  changePage(newPage: number): void {
    this.pageNumber = newPage;
    this.fetchProducts();
  }

  onProductClick(product: ProductResponseDto): void {
    this.selectedProductForModal = product;
  }
}
