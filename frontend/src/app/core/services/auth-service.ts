import { UserDto } from './../models/auth.model';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { LoginRequestDto, LoginResponseDto } from '../models/auth.model';
import { BehaviorSubject, Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
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

  private getUserFromStorage(): Omit<LoginResponseDto, 'token'> | null {
    const userJson = localStorage.getItem('rk_user');
    return userJson ? JSON.parse(userJson) : null;
  }

  private hasToken(): boolean {
    return !!localStorage.getItem('rk_token');
  }
}
