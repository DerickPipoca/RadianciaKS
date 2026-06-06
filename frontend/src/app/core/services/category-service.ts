import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CategoryRequestDto, CategoryResponseDto } from '../models/category.model';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private http = inject(HttpClient);
  private readonly endPoint = 'category';

  getAll(): Observable<CategoryResponseDto[]> {
    return this.http.get<CategoryResponseDto[]>(this.endPoint);
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
}
