import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Order } from '../../features/pdv/pages/order/order';
import { OrderResponseDto } from '../models/order.model';

@Injectable({
  providedIn: 'root',
})
export class PrintService {
  private http = inject(HttpClient);
  private readonly endPoint = 'printer';

  printReceiptSilent(order: OrderResponseDto): Observable<any> {
    const urlEndPoint = `${this.endPoint}/receipt`;
    return this.http.post(urlEndPoint, order);
  }
}
