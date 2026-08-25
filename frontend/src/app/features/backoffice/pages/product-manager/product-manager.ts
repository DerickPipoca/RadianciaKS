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
import { LucideAngularModule, TextSearch, Hamburger, Copy, ImagePlus } from 'lucide-angular';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ToastrService } from 'ngx-toastr';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-product-manager',
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    InputComponent,
    LucideAngularModule,
    Pagination,
    ModalComponent,
  ],
  templateUrl: './product-manager.html',
  styleUrl: './product-manager.scss',
})
export class ProductManager
  extends BaseCrud<ProductRequestDto, ProductResponseDto, string>
  implements OnInit
{
  readonly TextSearch = TextSearch;
  readonly Hamburger = Hamburger;
  readonly ImagePlus = ImagePlus;
  readonly Copy = Copy;

  searchSubject = new Subject<string>();

  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private modifierService = inject(ModifierService);
  private toastr = inject(ToastrService);

  categories: CategoryResponseDto[] = [];
  productModifiers: any[] = [];
  newGroup = { name: '', minChoices: 0, maxChoices: 1 };
  newOptions: {
    [groupId: string]: { name: string; price: number; description?: string; imagePath?: string };
  } = {};
  dropdownOpen = false;

  constructor() {
    super(inject(ChangeDetectorRef));
    this.searchSubject
      .pipe(debounceTime(500), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
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
    super.ngOnInit();
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
          this.toastr.success('Imagem enviada com sucesso!');
        },
        error: (err) => {
          console.error('Erro no upload', err);
          this.toastr.error('Falha ao enviar a imagem.');
        },
      });
    }
  }

  protected override validateSave(item: ProductRequestDto): boolean {
    if (!item.name || item.name.trim() === '') {
      this.toastr.warning('O nome do produto é obrigatório.');
      return false;
    }
    if (item.price == null || item.price < 0) {
      this.toastr.warning('O preço do produto deve ser maior ou igual a zero.');
      return false;
    }
    if (!item.categoryId) {
      this.toastr.warning('Selecione uma categoria para este produto.');
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

  descendingIcon(): string {
    return this.isDescending ? '↓' : '↑';
  }

  addGroup(): void {
    if (!this.newGroup.name.trim() || !this.editingId) {
      this.toastr.warning('Informe o nome do grupo de modificadores.');
      return;
    }

    const dto = {
      name: this.newGroup.name.trim(),
      minChoices: this.newGroup.minChoices,
      maxChoices: this.newGroup.maxChoices,
      productId: this.editingId,
    };

    this.modifierService.createGroup(dto as any).subscribe({
      next: (res) => {
        this.productModifiers.push({ ...res, options: [] });
        this.newGroup = { name: '', minChoices: 0, maxChoices: 1 };
        this.toastr.success('Grupo criado com sucesso!');
        this.loadData();
      },
      error: () => this.toastr.error('Erro ao criar grupo de modificadores.'),
    });
  }

  deleteGroup(groupId: string): void {
    if (confirm('Apagar este grupo e todas as suas opções?')) {
      this.modifierService.deleteGroup(groupId).subscribe({
        next: () => {
          this.productModifiers = this.productModifiers.filter((g) => g.id !== groupId);
          this.toastr.success('Grupo removido com sucesso!');
          this.loadData();
        },
        error: () => this.toastr.error('Erro ao remover grupo.'),
      });
    }
  }

  setOptionField(
    groupId: string,
    field: 'name' | 'description' | 'price' | 'imagePath',
    value: any,
  ): void {
    if (!this.newOptions[groupId]) {
      this.newOptions[groupId] = { name: '', price: 0, description: '', imagePath: '' };
    }
    (this.newOptions[groupId] as any)[field] = value;
  }

  onOptionImageSelected(event: any, groupId: string): void {
    const file: File = event.target.files[0];
    if (file) {
      this.productService.uploadImage(file).subscribe({
        next: (res) => {
          this.setOptionField(groupId, 'imagePath', res.url);
          this.toastr.success('Imagem do modificador enviada!');
        },
        error: (err) => {
          console.error('Erro no upload', err);
          this.toastr.error('Falha ao enviar a imagem do modificador.');
        },
      });
    }
  }

  addOption(groupId: string): void {
    const opt = this.newOptions[groupId];
    if (!opt || !opt.name.trim()) {
      this.toastr.warning('Informe o nome da opção.');
      return;
    }

    const dto = {
      name: opt.name.trim(),
      additionalPrice: opt.price || 0,
      description: opt.description || '',
      imagePath: opt.imagePath || '',
    };

    this.modifierService.addOptionToGroup(groupId, dto as any).subscribe({
      next: (res) => {
        const group = this.productModifiers.find((g) => g.id === groupId);
        if (group) {
          if (!group.options) group.options = [];
          group.options.push(res);
        }
        this.newOptions[groupId] = { name: '', price: 0, description: '' };
        this.toastr.success('Opção adicionada com sucesso!');
        this.loadData();
      },
      error: () => this.toastr.error('Erro ao adicionar opção.'),
    });
  }

  deleteOption(groupId: string, optionId: string): void {
    this.modifierService.deleteOption(optionId).subscribe({
      next: () => {
        const group = this.productModifiers.find((g) => g.id === groupId);
        if (group) {
          group.options = group.options.filter((o: any) => o.id !== optionId);
        }
        this.toastr.success('Opção removida!');
        this.loadData();
      },
      error: () => this.toastr.error('Erro ao remover opção.'),
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

  duplicateProduct(product: ProductResponseDto): void {
    if (confirm(`Deseja duplicar o produto "${product.name}" e todos os seus modificadores?`)) {
      this.productService.duplicate(product.id).subscribe({
        next: (newProduct: ProductResponseDto) => {
          this.toastr.success('Produto e modificadores duplicados com sucesso!');
          this.loadData();

          this.openEditModal(newProduct);
        },
        error: (err) => {
          console.error('Erro ao duplicar', err);
          this.toastr.error('Erro ao duplicar o produto.');
        },
      });
    }
  }
}
