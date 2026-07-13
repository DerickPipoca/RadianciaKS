import { ChangeDetectorRef, Directive, OnInit } from '@angular/core';
import { ICrudService } from '../interfaces/crud-service.interface';

@Directive()
export abstract class BaseCrud<TRequest, TResponse, TId = string> implements OnInit {
  dataList: TResponse[] = [];

  showModal = false;
  isEditing = false;

  currentItem: any = {};

  editingId: TId | null = null;

  constructor(private cdr: ChangeDetectorRef) {}

  protected abstract get crudService(): ICrudService<TRequest, TResponse, TId>;

  protected abstract getEmptyItem(): any;

  protected abstract getItemId(item: TResponse): TId;

  protected abstract mapToRequest(item: TResponse): TRequest;

  protected abstract validateSave(item: TRequest): boolean;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.crudService
      .getAll({
        pageNumber: 1,
        pageSize: 60,
      })
      .subscribe({
        next: (result) => {
          this.dataList = result.data ? result.data : result;
        },
        error: (err) => console.error('Erro ao carregar dados:', err),
      });
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
