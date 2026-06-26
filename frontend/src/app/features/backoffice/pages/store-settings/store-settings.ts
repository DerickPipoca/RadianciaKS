import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { StoreSettingsRequestDto } from '../../../../core/models/store-settings.model';
import { StoreSettingsService } from '../../../../core/services/store-settings-service';

@Component({
  selector: 'app-store-settings',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent],
  templateUrl: './store-settings.html',
  styleUrl: './store-settings.scss',
})
export class StoreSettings implements OnInit {
  private storeService = inject(StoreSettingsService);
  settings: StoreSettingsRequestDto = {
    storeName: '',
    cnpj: '',
    address: '',
    phone: '',
    receiptFooter: '',
    smallLogoPath: '',
    bigLogoPath: '',
    serviceCharge: 0,
  };

  ngOnInit() {
    this.storeService.getSettings().subscribe((data) => {
      this.settings = { ...data };
    });
  }

  onLogoSelected(event: any, isBig: boolean): void {
    const file = event.target.files[0];
    if (file) {
      this.storeService.uploadLogo(file, isBig).subscribe((res) => {
        if (isBig) this.settings.bigLogoPath = res.url;
        else this.settings.smallLogoPath = res.url;
      });
    }
  }

  save() {
    this.storeService.updateSettings(this.settings).subscribe(() => alert('Configurações salvas!'));
  }
}
