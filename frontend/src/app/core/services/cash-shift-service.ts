import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import {
  CashShiftHistory,
  CashShiftResponse,
  CloseCashShiftRequest,
  OpenCashShiftRequest,
} from '../models/cash-shift.model';

@Injectable({
  providedIn: 'root',
})
export class CashShiftService {
  private http = inject(HttpClient);
  private readonly endPoint = 'CashShift';

  private currentShiftSubject = new BehaviorSubject<CashShiftResponse | null>(null);
  public currentShift$ = this.currentShiftSubject.asObservable();

  getCurrentOpenShift(): Observable<CashShiftResponse | null> {
    const urlEndPoint = `${this.endPoint}/current`;
    return this.http
      .get<CashShiftResponse | null>(urlEndPoint)
      .pipe(tap((shift) => this.currentShiftSubject.next(shift)));
  }

  openShift(data: OpenCashShiftRequest): Observable<CashShiftResponse> {
    const urlEndPoint = `${this.endPoint}/open`;
    return this.http
      .post<CashShiftResponse>(urlEndPoint, data)
      .pipe(tap((shift) => this.currentShiftSubject.next(shift)));
  }

  closeShift(data: CloseCashShiftRequest): Observable<CashShiftResponse> {
    const urlEndPoint = `${this.endPoint}/close`;
    return this.http
      .post<CashShiftResponse>(urlEndPoint, data)
      .pipe(tap(() => this.currentShiftSubject.next(null)));
  }

  get currentShiftValue(): CashShiftResponse | null {
    return this.currentShiftSubject.value;
  }

  getHistory(): Observable<CashShiftHistory[]> {
    const urlEndPoint = `${this.endPoint}/history`;
    return this.http.get<CashShiftHistory[]>(urlEndPoint);
  }
}
