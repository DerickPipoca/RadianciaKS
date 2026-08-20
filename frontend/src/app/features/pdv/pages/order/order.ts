import { Component, inject, OnInit } from '@angular/core';
import { Catalog } from '../catalog/catalog';
import { Cart } from '../cart/cart';
import { CashShiftService } from '../../../../core/services/cash-shift-service';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, LockKeyholeIcon } from 'lucide-angular';

@Component({
  selector: 'app-order',
  imports: [CommonModule, Catalog, Cart, LucideAngularModule],
  templateUrl: './order.html',
  styleUrl: './order.scss',
})
export class Order implements OnInit {
  readonly LockKeyholeIcon = LockKeyholeIcon;
  private cashShiftService = inject(CashShiftService);

  hasOpenShift = false;
  isLoading = true;

  ngOnInit() {
    this.cashShiftService.currentShift$.subscribe((shift) => {
      this.hasOpenShift = !!shift;
      this.isLoading = false;
    });

    this.cashShiftService.getCurrentOpenShift().subscribe();
  }
}
