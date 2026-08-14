import { ChangeDetectorRef, Directive, OnInit } from '@angular/core';
import { ICrudService } from '../interfaces/crud-service.interface';

@Directive()
export abstract class BaseCrud<TRequest, TResponse, TId = string> implements OnInit {
  dataList: TResponse[] = [];
  showModal = false;
  isEditing = false;
  currentItem: any = {};
  editingId: TId | null = null;

  pageNumber: number = 1;
  pageSize: number = 12;
  totalRecords: number = 0;
  currentSortColumn: string = 'createdAt';
  isDescending: boolean = true;
  searchTerm: string = '';

  constructor(protected cdr: ChangeDetectorRef) {}

  protected abstract get crudService(): ICrudService<TRequest, TResponse, TId>;

  protected abstract getEmptyItem(): any;

  protected abstract getItemId(item: TResponse): TId;

  protected abstract mapToRequest(item: TResponse): TRequest;

  protected abstract validateSave(item: TRequest): boolean;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    const params = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm,
      sortBy: this.currentSortColumn,
      isDescending: this.isDescending,
    };

    this.crudService.getAll(params).subscribe({
      next: (result: any) => {
        this.dataList = result.data ? result.data : result;
        this.totalRecords = result.totalRecords || (Array.isArray(result) ? result.length : 0);
      },
      error: (err) => console.error('Erro ao carregar dados:', err),
    });
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && newPage <= this.getTotalPages()) {
      this.pageNumber = newPage;
      this.loadData();
    }
  }

  changeSort(column: string): void {
    if (this.currentSortColumn === column) {
      this.isDescending = !this.isDescending;
    } else {
      this.currentSortColumn = column;
      this.isDescending = true;
    }
    this.pageNumber = 1;
    this.loadData();
  }

  onSearchChange(term: string): void {
    this.searchTerm = term;
    this.pageNumber = 1;
    this.loadData();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalRecords / this.pageSize) || 1;
  }

  get rangeStart(): number {
    return this.totalRecords === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalRecords);
  }

  get pageNumbers(): number[] {
    const total = this.getTotalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  openNewModal(): void {
    this.isEditing = false;
    this.currentItem = this.getEmptyItem();
    this.showModal = true;
  }

  openEditModal(item: TResponse): void {
    this.isEditing = true;
    this.editingId = this.getItemId(item);
    this.currentItem = this.mapToRequest(item);
    this.showModal = true;
    this.cdr.detectChanges();
  }

  closeModal(): void {
    this.showModal = false;
  }

  save(): void {
    if (!this.validateSave(this.currentItem)) return;

    if (this.isEditing && this.editingId) {
      this.crudService.update(this.editingId, this.currentItem).subscribe({
        next: () => {
          this.loadData();
          this.closeModal();
        },
        error: (err) => alert('Erro ao atualizar o item.'),
      });
    } else {
      this.crudService.create(this.currentItem).subscribe({
        next: () => {
          this.loadData();
          this.closeModal();
        },
        error: (err) => alert('Erro ao criar o item.'),
      });
    }
  }

  delete(id: TId): void {
    if (confirm('Tem a certeza que deseja apagar este registo?')) {
      this.crudService.delete(id).subscribe({
        next: () => this.loadData(),
        error: (err) => alert('Erro ao apagar. Pode estar a ser usado noutro local.'),
      });
    }
  }
}
