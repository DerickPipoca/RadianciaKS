import { Injectable, signal } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class LoadingService {
  private activeRequests = 0;
  private loadingSubject = new BehaviorSubject<boolean>(false);

  public readonly loading$: Observable<boolean> = this.loadingSubject.asObservable();

  show(): void {
    this.activeRequests++;
    if (this.activeRequests === 1) {
      setTimeout(() => this.loadingSubject.next(true), 0);
    }
  }

  hide(): void {
    if (this.activeRequests > 0) {
      this.activeRequests--;
    }
    if (this.activeRequests === 0) {
      setTimeout(() => this.loadingSubject.next(false), 0);
    }
  }

  forceReset(): void {
    this.activeRequests = 0;
    setTimeout(() => this.loadingSubject.next(false), 0);
  }
}
