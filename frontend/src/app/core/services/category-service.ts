import { CategoryResponseDto, CategoryRequestDto } from './../models/category.model';
import { ICrudService } from './../interfaces/crud-service.interface';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CategoryService
  implements ICrudService<CategoryRequestDto, CategoryResponseDto, string>
{
  private http = inject(HttpClient);
  private readonly endPoint = 'category';

  getAll(): Observable<CategoryResponseDto[]> {
    return this.http.get<CategoryResponseDto[]>(this.endPoint);
  }

  getById(id: string): Observable<CategoryResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.get<CategoryResponseDto>(urlEndPoint);
  }

  create(category: CategoryRequestDto): Observable<CategoryResponseDto> {
    return this.http.post<CategoryResponseDto>(this.endPoint, category);
  }

  update(id: string, category: CategoryRequestDto): Observable<CategoryResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<CategoryResponseDto>(urlEndPoint, category);
  }

  delete(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }

  uploadImage(file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.endPoint}/upload-image`, formData);
  }
}
