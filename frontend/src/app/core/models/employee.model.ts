import { EmployeeRole } from '../enums/employee-role';

export interface EmployeeRequestDto {
  name: string;
  birthday?: string | null;
  cpf?: string;
  role: EmployeeRole;
  password?: string;
}

export interface EmployeeResponseDto {
  id: string;
  name: string;
  birthday?: string | null;
  cpf?: string;
  role: EmployeeRole;
}

export interface EmployeeBasicResponseDto {
  id: string;
  name: string;
  role: EmployeeRole;
}
