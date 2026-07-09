import { Component, inject, OnInit } from '@angular/core';
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
import { LucideAngularModule, TextSearch } from 'lucide-angular';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

@Component({
  selector: 'app-catalog',
  imports: [
    CommonModule,
    ModifierModal,
    RouterLink,
    FormsModule,
    InputComponent,
    LucideAngularModule,
  ],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
})
export class Catalog implements OnInit {
  TextSearch = TextSearch;

  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  categories: CategoryResponseDto[] = [];
  products: ProductResponseDto[] = [];
  filteredProducts: ProductResponseDto[] = [];

  searchTerm: string = '';
  pageNumber: number = 1;
  pageSize: number = 12;
  searchSubject = new Subject<string>();

  activeCategory: CategoryResponseDto | null = null;

  selectedProductForModal: ProductResponseDto | null = null;

  constructor() {
    this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
      this.pageNumber = 1;
      this.fetchProducts();
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Erro ao carregar categorias', err),
    });

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
          this.fetchProducts();
        },
        error: (err) => console.error('Erro ao carregar produtos', err),
      });
  }
  fetchProducts(): void {
    this.productService
      .getAll({
        pageNumber: this.pageNumber,
        pageSize: this.pageSize,
        searchTerm: this.searchTerm,
        categoryId: this.activeCategory?.id,
      })
      .subscribe({
        next: (response) => {
          this.products = response.data;
          this.filteredProducts = [...this.products];
        },
        error: (err) => console.error('Erro ao buscar produtos:', err),
      });
  }

  selectCategory(category: CategoryResponseDto): void {
    this.activeCategory = category;
    this.fetchProducts();
  }

  clearCategory(): void {
    this.activeCategory = null;
    this.pageNumber = 1;
    this.fetchProducts();
  }

  onProductClick(product: ProductResponseDto): void {
    if (product.modifierGroups && product.modifierGroups.length > 0) {
      this.selectedProductForModal = product;
    } else {
      this.cartService.addProduct(product);
    }
  }
}
