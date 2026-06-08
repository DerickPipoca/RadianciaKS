import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { ProductRequestDto, ProductResponseDto } from '../../../../core/models/product.model';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';
import { ProductService } from '../../../../core/services/product-service';
import { CategoryService } from '../../../../core/services/category-service';
import { CategoryResponseDto } from '../../../../core/models/category.model';

@Component({
  selector: 'app-product-manager',
  imports: [CommonModule, FormsModule],
  templateUrl: './product-manager.html',
  styleUrl: './product-manager.scss',
})
export class ProductManager
  extends BaseCrud<ProductRequestDto, ProductResponseDto, string>
  implements OnInit
{
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);

  categories: CategoryResponseDto[] = [];

  protected override get crudService(): ICrudService<
    ProductRequestDto,
    ProductResponseDto,
    string
  > {
    return this.productService;
  }
  protected override getEmptyItem(): ProductRequestDto {
    return {
      name: '',
      description: '',
      imagePath: '',
      price: 0,
      categoryId: '',
    };
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Erro ao carregar categorias para o produto:', err),
    });
  }

  protected override getItemId(item: ProductResponseDto): string {
    return item.id;
  }

  protected override mapToRequest(item: ProductResponseDto): ProductRequestDto {
    console.log('📦 Dados do produto recebidos do C#:', item);
    return {
      name: item.name,
      description: item.description ?? '',
      imagePath: item.imagePath ?? '',
      price: item.price,
      categoryId: item.categoryId,
    };
  }
  protected override validateSave(item: ProductRequestDto): boolean {
    if (!item.name || item.name.trim() === '') {
      alert('O nome do produto é obrigatório.');
      return false;
    }
    if (item.price == null || item.price < 0) {
      alert('O preço do produto deve ser um valor válido (maior ou igual a zero).');
      return false;
    }
    if (!item.categoryId) {
      alert('Selecione uma categoria para este produto.');
      return false;
    }
    return true;
  }
}
