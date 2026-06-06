import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ProductRequestDto, ProductResponse } from '../models/product.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
  private http = inject(HttpClient);
  private readonly endPoint = 'product';

  getAll(): Observable<ProductResponse[]> {
    return this.http.get<ProductResponse[]>(this.endPoint);
  }

  create(product: ProductRequestDto): Observable<ProductResponse> {
    return this.http.post<ProductResponse>(this.endPoint, product);
  }

  update(id: string, product: ProductRequestDto): Observable<ProductResponse> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<ProductResponse>(urlEndPoint, product);
  }

  delete(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }
}
