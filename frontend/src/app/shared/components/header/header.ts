import { EmployeeRole } from './../../../core/enums/employee-role';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth-service';
import { LucideAngularModule, Sparkle, Moon, Sun, LogOut } from 'lucide-angular';
import { Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../button-component/button-component';
import { ThemeService } from '../../../core/services/theme-service';
import { CashShiftService } from '../../../core/services/cash-shift-service';
import { SignalrService } from '../../../core/services/signalr-service';
import { AsyncPipe } from '@angular/common';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  imports: [LucideAngularModule, RouterLink, ButtonComponent, AsyncPipe],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class HeaderComponent implements OnInit, OnDestroy {
  public readonly Sparkle = Sparkle;
  public readonly LogOut = LogOut;
  public readonly Sun = Sun;
  public readonly Moon = Moon;

  public themeService = inject(ThemeService);
  public signalrService = inject(SignalrService);
  private cashShiftService = inject(CashShiftService);

  private router = inject(Router);
  authService = inject(AuthService);

  public isLoggedIn = false;
  isDropdownOpen = false;
  private authSubscription?: Subscription;

  ngOnInit(): void {
    this.authService.isLoggedIn$.subscribe((isLogged) => {
      this.isLoggedIn = isLogged;

      if (isLogged) {
        this.signalrService.startConnection();
        this.fetchInitialCashShift();
      }
    });

    if (this.authService.isAuthenticated()) {
      this.signalrService.startConnection();
      this.fetchInitialCashShift();
    }
  }

  ngOnDestroy(): void {
    this.authSubscription?.unsubscribe();
  }

  fetchInitialCashShift(): void {
    this.signalrService.cashShiftStatus$.next('Carregando...');

    this.cashShiftService.getCurrentOpenShift().subscribe({
      next: (shift) => {
        if (shift && !shift.closedAt) {
          this.signalrService.cashShiftStatus$.next('Aberto');
        } else {
          this.signalrService.cashShiftStatus$.next('Fechado');
        }
      },
      error: () => {
        this.signalrService.cashShiftStatus$.next('Fechado');
      },
    });
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  logout(): void {
    this.signalrService.stopConnection();
    this.authService.logout();
    this.isDropdownOpen = false;
    this.router.navigate(['login']);
  }

  getEmployeeRoleName(method: string | undefined): string {
    if (method) {
      const names: Record<string, string> = {
        Admin: 'Admin',
        Cashier: 'Caixa',
        Kitchen: 'Cozinha',
        Manager: 'Gerente',
        Waiter: 'Garçom',
      };
      return names[method];
    } else {
      return 'Funcionário';
    }
  }
}
