import { CategoryService } from './../../../../core/services/category-service';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryRequestDto, CategoryResponseDto } from '../../../../core/models/category.model';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';

@Component({
  selector: 'app-category-manager',
  imports: [CommonModule, FormsModule],
  templateUrl: './category-manager.html',
  styleUrl: './category-manager.scss',
})
export class CategoryManager extends BaseCrud<CategoryRequestDto, CategoryResponseDto, string> {
  private categoryService = inject(CategoryService);

  protected override get crudService(): ICrudService<
    CategoryRequestDto,
    CategoryResponseDto,
    string
  > {
    return this.categoryService;
  }

  protected override getEmptyItem(): CategoryRequestDto {
    return { name: '', imagePath: '', priority: 1 };
  }

  protected override getItemId(item: CategoryResponseDto): string {
    return item.id!;
  }

  protected override mapToRequest(item: CategoryResponseDto): CategoryRequestDto {
    return {
      name: item.name,
      imagePath: item.imagePath ?? '',
      priority: item.priority ?? 1,
    };
  }

  protected override validateSave(item: CategoryRequestDto): boolean {
    if (!item.name || item.name.trim() === '') {
      alert('O nome da categoria é obrigatório.');
      return false;
    }
    return true;
  }
}
