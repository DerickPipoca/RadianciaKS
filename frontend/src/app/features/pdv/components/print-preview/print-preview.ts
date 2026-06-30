import { CommonModule } from '@angular/common';
import { Component, inject, Input, OnInit } from '@angular/core';
import { OrderResponseDto } from '../../../../core/models/order.model';
import { StoreSettingsService } from '../../../../core/services/store-settings-service';
import { StoreSettingsResponseDto } from '../../../../core/models/store-settings.model';
import { PaymentMethod } from '../../../../core/enums/payment-method';
import * as QRCode from 'qrcode';

@Component({
  selector: 'app-print-preview',
  imports: [CommonModule],
  templateUrl: './print-preview.html',
  styleUrl: './print-preview.scss',
})
export class PrintPreview implements OnInit {
  @Input() order!: OrderResponseDto;

  public qrCodeDataUrl: string = '';

  private storeService = inject(StoreSettingsService);
  settings: StoreSettingsResponseDto | null = null;

  async ngOnInit() {
    if (this.order?.receiptUrl) {
      this.qrCodeDataUrl = await this.gerarQRCode(this.order.receiptUrl);
    }
    this.storeService.getSettings().subscribe({
      next: (data) => {
        this.settings = data;
      },
      error: (err) => console.error('Erro ao carregar configurações para recibo', err),
    });
  }

  getPaymentMethodName(method: PaymentMethod): string {
    const names: Record<number, string> = {
      [PaymentMethod.Cash]: 'Dinheiro',
      [PaymentMethod.CreditCard]: 'Crédito',
      [PaymentMethod.DebitCard]: 'Débito',
      [PaymentMethod.Pix]: 'PIX',
      [PaymentMethod.Other]: 'Outro',
    };
    return names[method] || 'Desconhecido';
  }

  calculateServiceCharge(): number {
    if (!this.settings || !this.order) return 0;
    return (this.order.totalAmount * (this.settings.serviceCharge ?? 0)) / 100;
  }

  print() {
    setTimeout(() => {
      window.print();
    }, 500);
  }

  async gerarQRCode(url: string) {
    try {
      const qrCodeDataUrl = await QRCode.toDataURL(url, {
        width: 200,
        margin: 1,
      });
      return qrCodeDataUrl;
    } catch (err) {
      console.error(err);
      return '';
    }
  }
}
