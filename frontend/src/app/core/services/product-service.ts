import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ProductRequestDto, ProductResponseDto } from '../models/product.model';
import { Observable } from 'rxjs';
import { ICrudService } from '../interfaces/crud-service.interface';

@Injectable({
  providedIn: 'root',
})
export class ProductService implements ICrudService<ProductRequestDto, ProductResponseDto, string> {
  private http = inject(HttpClient);
  private readonly endPoint = 'product';

  getAll(): Observable<ProductResponseDto[]> {
    return this.http.get<ProductResponseDto[]>(this.endPoint);
  }

  getById(id: string): Observable<ProductResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.get<ProductResponseDto>(urlEndPoint);
  }

  create(product: ProductRequestDto): Observable<ProductResponseDto> {
    return this.http.post<ProductResponseDto>(this.endPoint, product);
  }

  update(id: string, product: ProductRequestDto): Observable<ProductResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<ProductResponseDto>(urlEndPoint, product);
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
