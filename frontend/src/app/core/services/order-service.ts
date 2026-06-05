import { CheckoutRequest } from './../models/order.model';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { OrderItemRequest, OrderRequest, OrderResponse } from '../models/order.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class OrderService {
  private http = inject(HttpClient);
  private readonly endPoint = 'order';

  getAll(): Observable<OrderResponse[]> {
    return this.http.get<OrderResponse[]>(this.endPoint);
  }

  getById(id: string): Observable<OrderResponse> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.get<OrderResponse>(urlEndPoint);
  }

  create(order: OrderRequest): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(this.endPoint, order);
  }

  addItemToOrder(orderId: string, orderItem: OrderItemRequest): Observable<OrderResponse> {
    const urlEndPoint = `${this.endPoint}/${orderId}/items`;
    return this.http.post<OrderResponse>(urlEndPoint, orderItem);
  }

  checkoutOrder(orderId: string, checkout: CheckoutRequest): Observable<OrderResponse> {
    const urlEndPoint = `${this.endPoint}/${orderId}/checkout`;
    return this.http.put<OrderResponse>(urlEndPoint, checkout);
  }

  cancelOrder(orderId: string): Observable<OrderResponse> {
    const urlEndPoint = `${this.endPoint}/${orderId}/cancel`;
    return this.http.put<OrderResponse>(urlEndPoint, null);
  }

  update(id: string, order: OrderRequest): Observable<OrderResponse> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<OrderResponse>(urlEndPoint, order);
  }

  removeItemFromOrder(orderId: string, itemId: string): Observable<OrderResponse> {
    const urlEndPoint = `${this.endPoint}/${orderId}/items/${itemId}`;
    return this.http.delete<OrderResponse>(urlEndPoint);
  }
}
