import { Component, inject, OnInit } from '@angular/core';
import { CategoryService } from '../../../../core/services/category-service';
import { ProductService } from '../../../../core/services/product-service';
import { CategoryResponseDto } from '../../../../core/models/category.model';
import { ProductResponseDto } from '../../../../core/models/product.model';
import { CartService } from '../../../../core/services/cart-service';
import { CommonModule } from '@angular/common';
import { ModifierModal } from '../../components/modifier-modal/modifier-modal';

@Component({
  selector: 'app-catalog',
  imports: [CommonModule, ModifierModal],
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
  activeCategoryId: string | null = null;
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
        this.filteredProducts = this.products;
      },
      error: (err) => console.error('Erro ao carregar produtos', err),
    });
  }

  filterByCategory(categoryId: string | null): void {
    this.activeCategoryId = categoryId;

    if (!categoryId) {
      this.filteredProducts = this.products;
    } else {
      this.filteredProducts = this.products.filter((p) => p.categoryId === categoryId);
    }
  }

  onProductClick(product: ProductResponseDto): void {
    if (product.modifierGroups && product.modifierGroups.length > 0) {
      this.selectedProductForModal = product;
    } else {
      this.cartService.addProduct(product);
    }
  }
}
