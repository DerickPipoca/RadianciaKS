import { CheckoutRequestDto } from './../models/order.model';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { OrderItemRequestDto, OrderRequestDto, OrderResponseDto } from '../models/order.model';
import { Observable } from 'rxjs';
import { DashboardMetrics } from '../models/dashboard-metrics.model';

@Injectable({
  providedIn: 'root',
})
export class OrderService {
  private http = inject(HttpClient);
  private readonly endPoint = 'order';

  getAll(): Observable<OrderResponseDto[]> {
    return this.http.get<OrderResponseDto[]>(this.endPoint);
  }

  getById(id: string): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.get<OrderResponseDto>(urlEndPoint);
  }

  getDashboardMetrics(startDate: Date, endDate: Date): Observable<DashboardMetrics> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<DashboardMetrics>(`${this.endPoint}/metrics`, { params });
  }

  create(order: OrderRequestDto): Observable<OrderResponseDto> {
    return this.http.post<OrderResponseDto>(this.endPoint, order);
  }

  addItemToOrder(orderId: string, orderItem: OrderItemRequestDto): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/items`;
    return this.http.post<OrderResponseDto>(urlEndPoint, orderItem);
  }

  checkoutOrder(orderId: string, checkout: CheckoutRequestDto): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/checkout`;
    return this.http.post<OrderResponseDto>(urlEndPoint, checkout);
  }

  cancelOrder(orderId: string): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/cancel`;
    return this.http.put<OrderResponseDto>(urlEndPoint, null);
  }

  update(id: string, order: OrderRequestDto): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<OrderResponseDto>(urlEndPoint, order);
  }

  removeItemFromOrder(orderId: string, itemId: string): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/items/${itemId}`;
    return this.http.delete<OrderResponseDto>(urlEndPoint);
  }
}
