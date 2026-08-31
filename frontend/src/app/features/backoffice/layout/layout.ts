import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth-service';
import {
  LucideAngularModule,
  LayoutDashboard,
  BookOpen,
  History,
  Users,
  Settings,
  ShoppingCart,
  BaggageClaim,
} from 'lucide-angular';
import { CashShiftControlComponent } from '../components/cash-shift-control/cash-shift-control';

@Component({
  selector: 'app-layout',
  imports: [CommonModule, RouterModule, RouterLink, LucideAngularModule, CashShiftControlComponent],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout implements OnInit {
  readonly LayoutDashboard = LayoutDashboard;
  readonly BookOpen = BookOpen;
  readonly History = History;
  readonly Users = Users;
  readonly Settings = Settings;
  readonly ShoppingCart = ShoppingCart;
  readonly BaggageClaim = BaggageClaim;

  private authService = inject(AuthService);
  private router = inject(Router);

  userName = '';
  userRole = '';

  user = this.authService.currentUser;

  ngOnInit(): void {
    const user = this.authService.getUser();
    this.userName = user.name;
    this.userRole = user.role;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
