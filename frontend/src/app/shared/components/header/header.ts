import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth-service';
import { LucideAngularModule, Sparkle } from 'lucide-angular';
import { Router } from '@angular/router';
import { ButtonComponent } from '../button-component/button-component';

@Component({
  selector: 'app-header',
  imports: [LucideAngularModule, ButtonComponent],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class HeaderComponent implements OnInit {
  public readonly Sparkle = Sparkle;
  userName = '';
  userRole = '';

  private router = inject(Router);
  authService = inject(AuthService);

  user = this.authService.currentUser;

  isDropdownOpen = false;

  ngOnInit(): void {
    const user = this.authService.getUser();
    this.userName = user.name;
    this.userRole = user.role;
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  logout(): void {
    localStorage.removeItem('rk_user');
    localStorage.removeItem('rk_token');

    this.router.navigate(['login']);
    window.location.reload();
  }
}
