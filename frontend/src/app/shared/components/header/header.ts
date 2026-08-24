import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth-service';
import { LucideAngularModule, Sparkle, Moon, Sun } from 'lucide-angular';
import { Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../button-component/button-component';
import { ThemeService } from '../../../core/services/theme-service';
import { CashShiftService } from '../../../core/services/cash-shift-service';

@Component({
  selector: 'app-header',
  imports: [LucideAngularModule, RouterLink, ButtonComponent],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class HeaderComponent implements OnInit {
  public readonly Sparkle = Sparkle;
  public readonly Sun = Sun;
  public readonly Moon = Moon;

  public themeService = inject(ThemeService);
  private cashShiftService = inject(CashShiftService);

  public isLoggedIn = false;

  hasOpenShift = false;

  private router = inject(Router);
  authService = inject(AuthService);

  isDropdownOpen = false;

  ngOnInit(): void {
    this.authService.isLoggedIn$.subscribe((status) => {
      this.isLoggedIn = status;
    });

    this.cashShiftService.currentShift$.subscribe((shift) => {
      this.hasOpenShift = !!shift;
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
