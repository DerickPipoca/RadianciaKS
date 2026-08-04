import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, Banknote, Soup, Settings } from 'lucide-angular';
import { StoreSettingsService } from '../../core/services/store-settings-service';

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

  restaurantName = '';

  ngOnInit(): void {
    this.storeService.getSettings().subscribe((data) => {
      this.restaurantName = data.storeName;
    });
  }
}
