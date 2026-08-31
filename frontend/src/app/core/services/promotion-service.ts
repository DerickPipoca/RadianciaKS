import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { PromotionRequestDto, PromotionResponseDto } from '../models/promotion.model';
import { Observable } from 'rxjs';
import { PagedResponse } from '../models/paged-response.model';

@Injectable({
  providedIn: 'root',
})
export class PromotionService {
  private http = inject(HttpClient);
  private readonly endPoint = 'promotion';

  getActivePromotions(): Observable<PromotionResponseDto[]> {
    return this.http.get<PromotionResponseDto[]>(`${this.endPoint}/actives`);
  }

  getAll(params: {
    pageNumber: number;
    pageSize: number;
    searchTerm?: string;
    sortBy?: string;
    isDescending?: boolean;
  }): Observable<PagedResponse<PromotionResponseDto>> {
    let queryParams = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());

    if (params.searchTerm) queryParams = queryParams.set('searchTerm', params.searchTerm);
    if (params.sortBy) queryParams = queryParams.set('sortBy', params.sortBy);
    if (params.isDescending !== undefined)
      queryParams = queryParams.set('isDescending', params.isDescending.toString());

    return this.http.get<PagedResponse<PromotionResponseDto>>(this.endPoint, {
      params: queryParams,
    });
  }

  getById(id: string): Observable<PromotionResponseDto> {
    return this.http.get<PromotionResponseDto>(`${this.endPoint}/${id}`);
  }

  create(promotion: PromotionRequestDto): Observable<PromotionResponseDto> {
    return this.http.post<PromotionResponseDto>(this.endPoint, promotion);
  }

  update(id: string, promotion: PromotionRequestDto): Observable<PromotionResponseDto> {
    return this.http.put<PromotionResponseDto>(`${this.endPoint}/${id}`, promotion);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.endPoint}/${id}`);
  }

  toggleRunningStatus(id: string): Observable<boolean> {
    return this.http.patch<boolean>(`${this.endPoint}/${id}/toggle`, {});
  }
}
