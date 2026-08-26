import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth-service';
import { LucideAngularModule, Sparkle, Moon, Sun } from 'lucide-angular';
import { Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../button-component/button-component';
import { ThemeService } from '../../../core/services/theme-service';
import { CashShiftService } from '../../../core/services/cash-shift-service';
import { SignalrService } from '../../../core/services/signalr-service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-header',
  imports: [LucideAngularModule, RouterLink, ButtonComponent, AsyncPipe],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class HeaderComponent implements OnInit {
  public readonly Sparkle = Sparkle;
  public readonly Sun = Sun;
  public readonly Moon = Moon;

  public themeService = inject(ThemeService);
  public signalrService = inject(SignalrService);
  private cashShiftService = inject(CashShiftService);

  public isLoggedIn = false;

  private router = inject(Router);
  authService = inject(AuthService);

  isDropdownOpen = false;

  ngOnInit(): void {
    this.authService.isLoggedIn$.subscribe((status) => {
      this.isLoggedIn = status;
    });

    if (this.isLoggedIn) {
      this.signalrService.startConnection();
    } else {
      this.signalrService.stopConnection();
    }

    this.cashShiftService.getCurrentOpenShift().subscribe({
      next: (shift) => {
        if (shift && !shift.closedAt) {
          this.signalrService.cashShiftStatus$.next('Aberto');
        } else {
          this.signalrService.cashShiftStatus$.next('Fechado');
        }
      },
      error: () => this.signalrService.cashShiftStatus$.next('Fechado'),
    });

    this.cashShiftService.getCurrentOpenShift().subscribe();
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  logout(): void {
    this.authService.logout();
    this.isDropdownOpen = false;
    this.router.navigate(['login']);
  }
}
