import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { OrderItemResponseDto } from '../models/order.model';
import { KdsStatus } from '../enums/kds-status';

@Injectable({
  providedIn: 'root',
})
export class KdsService {
  private http = inject(HttpClient);
  private readonly endPoint = 'kds';

  getPendingItems(): Observable<OrderItemResponseDto[]> {
    return this.http.get<OrderItemResponseDto[]>(`${this.endPoint}/pending`);
  }

  updateItemStatus(orderId: string, itemId: string, status: KdsStatus): Observable<any> {
    return this.http.put(`${this.endPoint}/${orderId}/items/${itemId}/status`, status);
  }
}
