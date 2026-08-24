import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, Banknote, Soup, Settings } from 'lucide-angular';
import { StoreSettingsService } from '../../core/services/store-settings-service';
import { AuthService } from '../../core/services/auth-service';
import { EmployeeRole } from '../../core/enums/employee-role';

@Component({
  selector: 'app-landing-page',
  imports: [LucideAngularModule, RouterLink],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.scss',
})
export class LandingPage implements OnInit {
  readonly Banknote = Banknote;
  readonly Soup = Soup;
  readonly Settings = Settings;

  private storeService = inject(StoreSettingsService);
  private authService = inject(AuthService);

  restaurantName = '';

  ngOnInit(): void {
    this.storeService.getSettings().subscribe((data) => {
      this.restaurantName = data.storeName;
    });
  }

  hasAdminAccess(): boolean {
    const role = this.authService.currentUser()?.role;
    return (
      role === (EmployeeRole[EmployeeRole.Admin] as string) ||
      role === (EmployeeRole[EmployeeRole.Manager] as string)
    );
  }

  hasPdvAccess(): boolean {
    const role = this.authService.currentUser()?.role;
    return [
      EmployeeRole[EmployeeRole.Admin] as string,
      EmployeeRole[EmployeeRole.Manager] as string,
      EmployeeRole[EmployeeRole.Cashier] as string,
      EmployeeRole[EmployeeRole.Waiter] as string,
    ].includes(role || '');
  }

  hasKdsAccess(): boolean {
    const role = this.authService.currentUser()?.role;
    return [
      EmployeeRole[EmployeeRole.Admin] as string,
      EmployeeRole[EmployeeRole.Manager] as string,
      EmployeeRole[EmployeeRole.Kitchen] as string,
    ].includes(role || '');
  }
}
