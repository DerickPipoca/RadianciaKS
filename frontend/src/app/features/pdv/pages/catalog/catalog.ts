import { Component, inject, OnInit } from '@angular/core';
import { CategoryService } from '../../../../core/services/category-service';
import { ProductService } from '../../../../core/services/product-service';
import { CategoryResponse } from '../../../../core/models/category.model';
import { ProductResponse } from '../../../../core/models/product.model';
import { CartService } from '../../../../core/services/cart-service';

@Component({
  selector: 'app-catalog',
  imports: [],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
})
export class Catalog implements OnInit {
  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  categories: CategoryResponse[] = [];
  products: ProductResponse[] = [];
  filteredProducts: ProductResponse[] = [];
  activeCategoryId: string | null = null;

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

  onProductClick(product: ProductResponse): void {
    this.cartService.addProduct(product);
    console.log(`Adicionado: ${product.name}. Total no carrinho:`, this.cartService.subTotal());
  }
}
