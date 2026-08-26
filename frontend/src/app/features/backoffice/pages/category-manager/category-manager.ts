import { CategoryService } from './../../../../core/services/category-service';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryRequestDto, CategoryResponseDto } from '../../../../core/models/category.model';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { LucideAngularModule, TextSearch } from 'lucide-angular';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-category-manager',
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    InputComponent,
    LucideAngularModule,
    ModalComponent,
  ],
  templateUrl: './category-manager.html',
  styleUrl: './category-manager.scss',
})
export class CategoryManager extends BaseCrud<CategoryRequestDto, CategoryResponseDto, string> {
  private toastr = inject(ToastrService);
  TextSearch = TextSearch;

  private categoryService = inject(CategoryService);

  searchSubject = new Subject<string>();

  private allCategories: CategoryResponseDto[] = [];

  constructor() {
    super(inject(ChangeDetectorRef));
    this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
      this.filterCategories(term);
    });
  }

  override loadData(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.allCategories = data;
        this.dataList = data;
      },
      error: (err) => console.error('Erro ao carregar:', err),
    });
  }

  filterCategories(term: string): void {
    if (!term.trim()) {
      this.dataList = [...this.allCategories];
    } else {
      const lowerTerm = term.toLowerCase();
      this.dataList = this.allCategories.filter((cat) =>
        cat.name.toLowerCase().includes(lowerTerm),
      );
    }
  }

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
      this.toastr.warning('O nome da categoria é obrigatório.');
      return false;
    }
    this.toastr.success('Categoria salva com sucesso!');
    return true;
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      this.categoryService.uploadImage(file).subscribe({
        next: (res) => {
          this.currentItem.imagePath = res.url;
        },
        error: (err) => {
          console.error('Erro no upload', err);
          this.toastr.info('Falha ao enviar a imagem.');
        },
      });
    }
  }
}
