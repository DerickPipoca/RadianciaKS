import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BaseCrud } from '../../../../core/classes/base-crud';
import { EmployeeRequestDto, EmployeeResponseDto } from '../../../../core/models/employee.model';
import { ICrudService } from '../../../../core/interfaces/crud-service.interface';
import { EmployeeService } from '../../../../core/services/employee-service';
import { EmployeeRole } from '../../../../core/enums/employee-role';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';

@Component({
  selector: 'app-employee-manager',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent],
  templateUrl: './employee-manager.html',
  styleUrl: './employee-manager.scss',
})
export class EmployeeManager extends BaseCrud<EmployeeRequestDto, EmployeeResponseDto, string> {
  private employeeService = inject(EmployeeService);

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

  override loadData(): void {
    this.employeeService.getAll().subscribe({
      next: (result) => (this.dataList = result as any[]),
      error: (err) => console.error('Erro ao carregar dados:', err),
    });
  }

  override openEditModal(item: any): void {
    this.employeeService.getById(item.id).subscribe({
      next: (fullEmployeeDetails) => {
        super.openEditModal(fullEmployeeDetails);
      },
      error: () => alert('Erro ao carregar os detalhes do funcionário.'),
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
      alert('O nome do funcionário é obrigatório.');
      return false;
    }

    if (!this.isEditing && (!item.password || item.password.trim() === '')) {
      alert('A senha é obrigatória ao criar um novo funcionário.');
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
