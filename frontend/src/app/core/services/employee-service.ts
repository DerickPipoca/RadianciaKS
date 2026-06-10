import { inject, Injectable } from '@angular/core';
import {
  EmployeeBasicResponseDto,
  EmployeeRequestDto,
  EmployeeResponseDto,
} from '../models/employee.model';
import { Observable } from 'rxjs';
import { IEmployeeService } from '../interfaces/employee-service.interface';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class EmployeeService implements IEmployeeService {
  private http = inject(HttpClient);
  private readonly endPoint = 'employee';

  getAll(): Observable<EmployeeBasicResponseDto[]> {
    return this.http.get<EmployeeBasicResponseDto[]>(this.endPoint);
  }
  getBasicInformationById(id: string): Observable<EmployeeBasicResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.get<EmployeeBasicResponseDto>(urlEndPoint);
  }
  getById(id: string): Observable<EmployeeResponseDto> {
    const urlEndPoint = `${this.endPoint}/admin/${id}`;
    return this.http.get<EmployeeResponseDto>(urlEndPoint);
  }
  create(employee: EmployeeRequestDto): Observable<EmployeeResponseDto> {
    return this.http.post<EmployeeResponseDto>(this.endPoint, employee);
  }
  update(id: string, employee: EmployeeRequestDto): Observable<EmployeeResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<EmployeeResponseDto>(urlEndPoint, employee);
  }
  delete(id: string): Observable<any> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }
}
