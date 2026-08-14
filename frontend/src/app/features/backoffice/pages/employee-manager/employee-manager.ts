import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { EmployeeRequestDto, EmployeeResponseDto } from '../../../../core/models/employee.model';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';
import { EmployeeService } from '../../../../core/services/employee-service';
import { EmployeeRole } from '../../../../core/enums/employee-role';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { LucideAngularModule, TextSearch } from 'lucide-angular';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-employee-manager',
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    InputComponent,
    LucideAngularModule,
    ModalComponent,
  ],
  templateUrl: './employee-manager.html',
  styleUrl: './employee-manager.scss',
})
export class EmployeeManager extends BaseCrud<EmployeeRequestDto, EmployeeResponseDto, string> {
  TextSearch = TextSearch;

  private employeeService = inject(EmployeeService);
  private toastr = inject(ToastrService);

  searchSubject = new Subject<string>();

  private allEmployees: EmployeeResponseDto[] = [];

  constructor() {
    super(inject(ChangeDetectorRef));
    this.searchSubject.pipe(debounceTime(500), distinctUntilChanged()).subscribe((term) => {
      this.filterEmployees(term);
    });
  }

  override loadData(): void {
    this.employeeService.getAll().subscribe({
      next: (data) => {
        this.allEmployees = data;
        this.dataList = data;
      },
      error: (err) => {
        console.error('Erro ao carregar:', err);
        this.toastr.error('Falha ao carregar lista de funcionários.');
      },
    });
  }

  filterEmployees(term: string): void {
    if (!term.trim()) {
      this.dataList = [...this.allEmployees];
    } else {
      const lowerTerm = term.toLowerCase();
      this.dataList = this.allEmployees.filter((emp) => emp.name.toLowerCase().includes(lowerTerm));
    }
  }

  dropdownOpen = false;

  roleOptions = [
    { value: EmployeeRole.Admin, label: 'Administrador (Acesso Total)' },
    { value: EmployeeRole.Manager, label: 'Gerente' },
    { value: EmployeeRole.Cashier, label: 'Caixa / PDV' },
    { value: EmployeeRole.Waiter, label: 'Garçom / Mesa' },
    { value: EmployeeRole.Kitchen, label: 'Cozinha / KDS' },
  ];

  protected override get crudService(): any {
    return this.employeeService;
  }

  override openEditModal(item: any): void {
    this.employeeService.getById(item.id).subscribe({
      next: (fullEmployeeDetails) => {
        super.openEditModal(fullEmployeeDetails);
      },
      error: () => this.toastr.error('Erro ao carregar os detalhes do funcionário.'),
    });
  }

  protected override getEmptyItem(): EmployeeRequestDto {
    return { name: '', role: EmployeeRole.Cashier, cpf: '', birthday: null, password: '' };
  }
  protected override getItemId(item: EmployeeResponseDto): string {
    return item.id;
  }
  protected override mapToRequest(item: EmployeeResponseDto): EmployeeRequestDto {
    return {
      name: item.name,
      cpf: item.cpf ?? '',
      birthday: item.birthday ?? null,
      role: item.role,
      password: '',
    };
  }
  protected override validateSave(item: EmployeeRequestDto): boolean {
    console.log(item);
    if (!item.name || item.name.trim() === '') {
      this.toastr.warning('O nome do funcionário é obrigatório.');
      return false;
    }

    if (!this.isEditing && (!item.password || item.password.trim() === '')) {
      this.toastr.warning('A senha é obrigatória ao criar um novo funcionário.');
      return false;
    }
    return true;
  }

  getRoleLabel(roleValue: number): string {
    return this.roleOptions.find((r) => r.value === roleValue)?.label || 'Desconhecido';
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  selectRole(role: any) {
    this.currentItem.role = role;
    this.dropdownOpen = false;
  }
}
