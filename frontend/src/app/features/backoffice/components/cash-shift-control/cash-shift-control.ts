import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { CashShiftService } from '../../../../core/services/cash-shift-service';
import { LucideAngularModule, TriangleAlert } from 'lucide-angular';

@Component({
  selector: 'app-cash-shift-control',
  imports: [
    CommonModule,
    FormsModule,
    ModalComponent,
    ButtonComponent,
    InputComponent,
    LucideAngularModule,
  ],
  templateUrl: './cash-shift-control.html',
  styleUrl: './cash-shift-control.scss',
})
export class CashShiftControlComponent implements OnInit {
  readonly TriangleAlert = TriangleAlert;

  private cashShiftService = inject(CashShiftService);

  hasOpenShift = false;
  pendingOrdersCount = 0;
  isSubmitting = false;

  showOpenModal = false;
  initialBalance: number = 0;

  showCloseModal = false;
  finalReportedBalance: number = 0;

  ngOnInit() {
    this.cashShiftService.currentShift$.subscribe((shift: any) => {
      this.hasOpenShift = !!shift;
      this.pendingOrdersCount = shift?.pendingOrdersCount ?? 0;
    });

    this.cashShiftService.getCurrentOpenShift().subscribe();
  }

  promptAction() {
    if (this.hasOpenShift) {
      this.finalReportedBalance = 0;
      this.cashShiftService.getCurrentOpenShift().subscribe({
        next: (shift: any) => {
          this.pendingOrdersCount = shift?.pendingOrdersCount ?? 0;
          this.showCloseModal = true;
        },
        error: () => {
          this.showCloseModal = true;
        },
      });
    } else {
      this.initialBalance = 0;
      this.showOpenModal = true;
    }
  }

  openShift() {
    if (this.initialBalance < 0 || this.isSubmitting) return;

    this.isSubmitting = true;
    this.cashShiftService.openShift({ initialBalance: this.initialBalance }).subscribe({
      next: () => {
        this.showOpenModal = false;
        this.isSubmitting = false;
      },
      error: (err) => {
        this.isSubmitting = false;
      },
    });
  }

  closeShift() {
    if (this.finalReportedBalance < 0 || this.pendingOrdersCount > 0 || this.isSubmitting) return;

    this.isSubmitting = true;
    this.cashShiftService
      .closeShift({ finalReportedBalance: this.finalReportedBalance })
      .subscribe({
        next: () => {
          this.showCloseModal = false;
          this.isSubmitting = false;
        },
        error: (err) => {
          this.isSubmitting = false;
        },
      });
  }
}
