import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { ProductRequestDto, ProductResponseDto } from '../../../../core/models/product.model';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';
import { ProductService } from '../../../../core/services/product-service';
import { CategoryService } from '../../../../core/services/category-service';
import { CategoryResponseDto } from '../../../../core/models/category.model';
import { ModifierService } from '../../../../core/services/modifier-service';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';

@Component({
  selector: 'app-product-manager',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent],
  templateUrl: './product-manager.html',
  styleUrl: './product-manager.scss',
})
export class ProductManager
  extends BaseCrud<ProductRequestDto, ProductResponseDto, string>
  implements OnInit
{
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private modifierService = inject(ModifierService);

  categories: CategoryResponseDto[] = [];
  productModifiers: any[] = [];
  newGroup = { name: '', minChoices: 0, maxChoices: 1 };
  newOptions: { [groupId: string]: { name: string; price: number; description?: string } } = {};
  dropdownOpen = false;

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
    console.log('Dados do produto recebidos do C#:', item);
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

  override openEditModal(item: ProductResponseDto): void {
    super.openEditModal(item);

    this.productModifiers = item.modifierGroups
      ? JSON.parse(JSON.stringify(item.modifierGroups))
      : [];
  }

  override closeModal(): void {
    super.closeModal();
    this.dropdownOpen = false;
  }

  addGroup(): void {
    if (!this.newGroup.name || !this.editingId) return;

    const dto = {
      name: this.newGroup.name,
      minChoices: this.newGroup.minChoices,
      maxChoices: this.newGroup.maxChoices,
      productId: this.editingId,
    };

    this.modifierService.createGroup(dto as any).subscribe({
      next: (res) => {
        this.productModifiers.push({ ...res, options: [] });
        this.newGroup = { name: '', minChoices: 0, maxChoices: 1 };
        this.loadData();
      },
      error: () => alert('Erro ao criar grupo.'),
    });
  }

  deleteGroup(groupId: string): void {
    if (confirm('Apagar este grupo e todas as suas opções?')) {
      this.modifierService.deleteGroup(groupId).subscribe({
        next: () => {
          this.productModifiers = this.productModifiers.filter((g) => g.id !== groupId);
          this.loadData();
        },
      });
    }
  }

  addOption(groupId: string): void {
    if (!this.newOptions[groupId])
      this.newOptions[groupId] = { name: '', price: 0, description: '' };

    const opt = this.newOptions[groupId];
    if (!opt.name) return;

    const dto = { name: opt.name, additionalPrice: opt.price, description: opt.description };

    this.modifierService.addOptionToGroup(groupId, dto as any).subscribe({
      next: (res) => {
        const group = this.productModifiers.find((g) => g.id === groupId);
        if (group) {
          if (!group.options) group.options = [];
          group.options.push(res);
        }
        this.newOptions[groupId] = { name: '', price: 0, description: '' };
        this.loadData();
      },
      error: () => alert('Erro ao adicionar opção.'),
    });
  }

  deleteOption(groupId: string, optionId: string): void {
    this.modifierService.deleteOption(optionId).subscribe({
      next: () => {
        const group = this.productModifiers.find((g) => g.id === groupId);
        if (group) {
          group.options = group.options.filter((o: any) => o.id !== optionId);
        }
        this.loadData();
      },
    });
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  selectCategory(id: any) {
    this.currentItem.categoryId = id;
    this.dropdownOpen = false;
  }

  getSelectedCategoryName(): string {
    const selected = this.categories?.find((cat) => cat.id === this.currentItem.categoryId);
    return selected ? selected.name : '';
  }
}
