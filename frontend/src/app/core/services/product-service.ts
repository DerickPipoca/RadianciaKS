import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ProductRequestDto, ProductResponseDto } from '../models/product.model';
import { Observable } from 'rxjs';
import { PagedResponse } from '../models/paged-response.model';
import { IProductService } from '../interfaces/product-service.interface';

@Injectable({
  providedIn: 'root',
})
export class ProductService implements IProductService {
  private http = inject(HttpClient);
  private readonly endPoint = 'product';

  getAll(params: {
    pageNumber: number;
    pageSize: number;
    searchTerm?: string;
    sortBy?: string;
    isDescending?: boolean;
    categoryId?: string;
  }): Observable<PagedResponse<ProductResponseDto>> {
    let queryParams = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());

    if (params.searchTerm) queryParams = queryParams.set('searchTerm', params.searchTerm);
    if (params.sortBy) queryParams = queryParams.set('sortBy', params.sortBy);
    if (params.categoryId) queryParams = queryParams.set('categoryId', params.categoryId);
    if (params.isDescending !== undefined)
      queryParams = queryParams.set('isDescending', params.isDescending.toString());

    return this.http.get<PagedResponse<ProductResponseDto>>(this.endPoint, {
      params: queryParams,
    });
  }

  getById(id: string): Observable<ProductResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.get<ProductResponseDto>(urlEndPoint);
  }

  duplicate(id: string): Observable<ProductResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}/duplicate`;
    return this.http.post<ProductResponseDto>(urlEndPoint, {});
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
}
