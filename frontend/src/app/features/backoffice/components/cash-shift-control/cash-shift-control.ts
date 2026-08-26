import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { InputComponent } from '../../../../shared/components/input-component/input-component';
import { CashShiftService } from '../../../../core/services/cash-shift-service';

@Component({
  selector: 'app-cash-shift-control',
  imports: [CommonModule, FormsModule, ModalComponent, ButtonComponent, InputComponent],
  templateUrl: './cash-shift-control.html',
  styleUrl: './cash-shift-control.scss',
})
export class CashShiftControlComponent implements OnInit {
  private cashShiftService = inject(CashShiftService);

  hasOpenShift = false;

  showOpenModal = false;
  initialBalance: number = 0;

  showCloseModal = false;
  finalReportedBalance: number = 0;

  ngOnInit() {
    this.cashShiftService.currentShift$.subscribe((shift) => {
      this.hasOpenShift = !!shift;
    });

    this.cashShiftService.getCurrentOpenShift().subscribe();
  }

  promptAction() {
    if (this.hasOpenShift) {
      this.finalReportedBalance = 0;
      this.showCloseModal = true;
    } else {
      this.initialBalance = 0;
      this.showOpenModal = true;
    }
  }

  openShift() {
    if (this.initialBalance < 0) return;

    this.cashShiftService.openShift({ initialBalance: this.initialBalance }).subscribe({
      next: () => {
        this.showOpenModal = false;
      }
    });
  }

  closeShift() {
    if (this.finalReportedBalance < 0) return;

    this.cashShiftService
      .closeShift({ finalReportedBalance: this.finalReportedBalance })
      .subscribe({
        next: () => {
          this.showCloseModal = false;
        }
      });
  }
}
