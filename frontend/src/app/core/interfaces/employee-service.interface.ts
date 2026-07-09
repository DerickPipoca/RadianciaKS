import {
  EmployeeBasicResponseDto,
  EmployeeRequestDto,
  EmployeeResponseDto,
} from './../models/employee.model';
import { Observable } from 'rxjs';
import { ICrudService } from './crud-service.interface';

export interface IEmployeeService
  extends Omit<ICrudService<EmployeeRequestDto, EmployeeResponseDto, string>, 'getAll'> {
  getAll(params: any): Observable<EmployeeBasicResponseDto[]>;

  getBasicInformationById(id: string): Observable<EmployeeBasicResponseDto>;
}
