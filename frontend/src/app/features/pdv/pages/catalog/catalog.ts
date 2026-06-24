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

@Component({
  selector: 'app-catalog',
  imports: [CommonModule, ModifierModal, RouterLink, FormsModule],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
})
export class Catalog implements OnInit {
  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  categories: CategoryResponseDto[] = [];
  products: ProductResponseDto[] = [];
  filteredProducts: ProductResponseDto[] = [];

  searchTerm: string = '';

  activeCategory: CategoryResponseDto | null = null;

  selectedProductForModal: ProductResponseDto | null = null;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Erro ao carregar categorias', err),
    });

    this.productService.getAll().subscribe({
      next: (data) => {
        this.products = data;
        this.applyFilters();
      },
      error: (err) => console.error('Erro ao carregar produtos', err),
    });
  }

  applyFilters(): void {
    const term = this.searchTerm.toLowerCase().trim();

    this.filteredProducts = this.products.filter((p) => {
      const matchesCategory = this.activeCategory ? p.categoryId === this.activeCategory.id : true;

      const matchesSearch =
        term === '' ||
        p.name.toLowerCase().includes(term) ||
        (p.description && p.description.toLowerCase().includes(term));

      return matchesCategory && matchesSearch;
    });
  }

  selectCategory(category: CategoryResponseDto): void {
    this.activeCategory = category;
    this.applyFilters();
  }

  clearCategory(): void {
    this.activeCategory = null;
    this.applyFilters();
  }

  onProductClick(product: ProductResponseDto): void {
    if (product.modifierGroups && product.modifierGroups.length > 0) {
      this.selectedProductForModal = product;
    } else {
      this.cartService.addProduct(product);
    }
  }
}
