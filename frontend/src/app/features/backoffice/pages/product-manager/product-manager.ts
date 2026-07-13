import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
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
import { LucideAngularModule, TextSearch } from 'lucide-angular';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

@Component({
  selector: 'app-product-manager',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent, LucideAngularModule],
  templateUrl: './product-manager.html',
  styleUrl: './product-manager.scss',
})
export class ProductManager
  extends BaseCrud<ProductRequestDto, ProductResponseDto, string>
  implements OnInit
{
  TextSearch = TextSearch;

  searchSubject = new Subject<string>();

  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private modifierService = inject(ModifierService);

  categories: CategoryResponseDto[] = [];
  productModifiers: any[] = [];
  newGroup = { name: '', minChoices: 0, maxChoices: 1 };
  newOptions: { [groupId: string]: { name: string; price: number; description?: string } } = {};
  dropdownOpen = false;

  pageNumber: number = 1;
  pageSize: number = 12;
  totalRecords: number = 0;
  searchTerm: string = '';
  sortBy: string = '';
  descendingSort: boolean = false;

  constructor() {
    super(inject(ChangeDetectorRef));
    this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
      this.searchTerm = term;
      this.pageNumber = 1;
      this.loadData();
    });
  }

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
    this.loadCategories();
    this.loadData();
  }

  override loadData(): void {
    this.productService
      .getAll({
        pageNumber: this.pageNumber,
        pageSize: this.pageSize,
        isDescending: this.descendingSort,
        sortBy: this.sortBy,
        searchTerm: this.searchTerm,
      })
      .subscribe({
        next: (response) => {
          this.dataList = response.data;
          this.totalRecords = response.totalRecords;
        },
        error: (err) => console.error('Erro ao carregar produtos paginados:', err),
      });
  }

  changePage(newPage: number): void {
    if (newPage < 1 || newPage > this.getTotalPages()) return;
    this.pageNumber = newPage;
    this.loadData();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalRecords / this.pageSize) || 1;
  }

  get pageNumbers(): number[] {
    const total = this.getTotalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  onSearchChange(term: string): void {
    this.searchTerm = term;
    this.pageNumber = 1;
    this.loadData();
  }

  get rangeStart(): number {
    return this.totalRecords === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    const end = this.pageNumber * this.pageSize;
    return end > this.totalRecords ? this.totalRecords : end;
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

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      this.productService.uploadImage(file).subscribe({
        next: (res) => {
          this.currentItem.imagePath = res.url;
          console.log('Upload concluído:', res.url);
        },
        error: (err) => {
          console.error('Erro no upload', err);
          alert('Falha ao enviar a imagem.');
        },
      });
    }
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

  orderBy(sortBy: string): void {
    if (this.sortBy !== sortBy) {
      this.sortBy = sortBy;
      this.descendingSort = false;
    } else {
      this.descendingSort = !this.descendingSort;
    }
    this.loadData();
  }

  isOrderBy(sortBy: string): boolean {
    if (this.sortBy === sortBy) {
      return true;
    }
    return false;
  }

  descendingIcon(): string {
    return this.descendingSort ? '↓' : '↑';
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
