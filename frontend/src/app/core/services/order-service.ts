import { PaymentStatus } from './../enums/payment-status';
import { CheckoutRequestDto } from './../models/order.model';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { OrderItemRequestDto, OrderRequestDto, OrderResponseDto } from '../models/order.model';
import { Observable } from 'rxjs';
import { DashboardMetrics } from '../models/dashboard-metrics.model';
import { PagedResponse } from '../models/paged-response.model';
import { OrderStatus } from '../enums/order-status';

@Injectable({
  providedIn: 'root',
})
export class OrderService {
  private http = inject(HttpClient);
  private readonly endPoint = 'order';

  getAll(params: {
    pageNumber: number;
    pageSize: number;
    searchTerm?: string;
    sortBy?: string;
    isDescending?: boolean;
    status?: OrderStatus;
    paymentStatus?: PaymentStatus;
  }): Observable<PagedResponse<OrderResponseDto>> {
    let queryParams = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString());

    if (params.searchTerm) queryParams = queryParams.set('searchTerm', params.searchTerm);
    if (params.sortBy) queryParams = queryParams.set('sortBy', params.sortBy);
    if (params.isDescending !== undefined)
      queryParams = queryParams.set('isDescending', params.isDescending.toString());
    if (params.status) queryParams = queryParams.set('orderStatus', params.status);
    if (params.paymentStatus) queryParams = queryParams.set('paymentStatus', params.paymentStatus);

    return this.http.get<PagedResponse<OrderResponseDto>>(`${this.endPoint}`, {
      params: queryParams,
    });
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
    const urlEndPoint = `${this.endPoint}/${orderId}/item`;
    return this.http.post<OrderResponseDto>(urlEndPoint, orderItem);
  }

  addItemsToOrder(orderId: string, orderItems: OrderItemRequestDto[]): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/items`;
    return this.http.post<OrderResponseDto>(urlEndPoint, orderItems);
  }

  checkoutOrder(orderId: string, checkout: CheckoutRequestDto): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/checkout`;
    return this.http.post<OrderResponseDto>(urlEndPoint, checkout);
  }

  cancelOrder(orderId: string): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/cancel`;
    return this.http.put<OrderResponseDto>(urlEndPoint, null);
  }

  deliverOrder(orderId: string): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/deliver`;
    return this.http.put<OrderResponseDto>(urlEndPoint, null);
  }

  update(id: string, order: OrderRequestDto): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${id}`;
    return this.http.put<OrderResponseDto>(urlEndPoint, order);
  }

  removeItemFromOrder(orderId: string, itemId: string): Observable<OrderResponseDto> {
    const urlEndPoint = `${this.endPoint}/${orderId}/item/${itemId}`;
    return this.http.delete<OrderResponseDto>(urlEndPoint);
  }
}
