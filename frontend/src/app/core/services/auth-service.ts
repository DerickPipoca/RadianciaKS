import { UserDto } from './../models/auth.model';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { LoginRequestDto, LoginResponseDto } from '../models/auth.model';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly apiUrl = 'auth';

  private loggedInSubject = new BehaviorSubject<boolean>(this.hasToken());

  public isLoggedIn$ = this.loggedInSubject.asObservable();

  currentUser = signal<Omit<LoginResponseDto, 'token'> | null>(this.getUserFromStorage());

  login(dto: LoginRequestDto): Observable<LoginResponseDto> {
    return this.http.post<LoginResponseDto>(`${this.apiUrl}/login`, dto).pipe(
      tap((response) => {
        localStorage.setItem('rk_token', response.token);
        localStorage.setItem(
          'rk_user',
          JSON.stringify({ name: response.name, role: response.role }),
        );

        this.currentUser.set({ name: response.name, role: response.role });

        this.loggedInSubject.next(true);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem('rk_token');
    localStorage.removeItem('rk_user');
    this.currentUser.set(null);
    this.loggedInSubject.next(false);

    if (this.router.url !== '/login') {
      this.router.navigate(['/login']);
    }
  }

  getToken(): string | null {
    return localStorage.getItem('rk_token');
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  getUser(): UserDto {
    const userJson = localStorage.getItem('rk_user');
    const parsed = userJson ? JSON.parse(userJson) : null;

    const user: UserDto = {
      name: parsed?.name ?? '',
      role: parsed?.role ?? '',
    };

    return user;
  }

  private isTokenValid(token: string | null): boolean {
    if (!token) return false;

    try {
      const parts = token.split('.');
      if (parts.length !== 3) return false;

      const payloadBase64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(atob(payloadBase64));

      if (!payload.exp) return true;

      const nowInSeconds = Math.floor(Date.now() / 1000);
      return payload.exp > nowInSeconds;
    } catch {
      return false;
    }
  }

  private hasValidToken(): boolean {
    const token = this.getToken();
    const valid = this.isTokenValid(token);

    if (!valid && token) {
      this.logout();
    }

    return valid;
  }

  private getUserFromStorage(): Omit<LoginResponseDto, 'token'> | null {
    if (!this.hasValidToken()) {
      return null;
    }

    const userJson = localStorage.getItem('rk_user');
    return userJson ? JSON.parse(userJson) : null;
  }

  private hasToken(): boolean {
    return !!localStorage.getItem('rk_token');
  }
}
