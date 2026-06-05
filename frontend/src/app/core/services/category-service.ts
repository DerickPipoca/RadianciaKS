import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CategoryRequest, CategoryResponse } from '../models/category.model';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private http = inject(HttpClient);
  private readonly endPoint = 'category';

  getAll(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(this.endPoint);
  }

  create(category: CategoryRequest): Observable<CategoryResponse> {
    return this.http.post<CategoryResponse>(this.endPoint, category);
  }

  update(id: string, category: CategoryRequest): Observable<CategoryResponse> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<CategoryResponse>(urlEndPoint, category);
  }

  delete(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }
}
