import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, ViewEncapsulation } from '@angular/core';

@Component({
  selector: 'app-modal-component',
  imports: [CommonModule],
  templateUrl: './modal-component.html',
  styleUrl: './modal-component.scss',
  encapsulation: ViewEncapsulation.None
})
export class ModalComponent {
  @Input() title: string = '';
  @Output() onClose = new EventEmitter<void>();

  close() {
    this.onClose.emit();
  }
}
