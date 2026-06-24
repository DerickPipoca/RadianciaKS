import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input-component',
  imports: [FormsModule],
  templateUrl: './input-component.html',
  styleUrl: './input-component.scss',
})
export class InputComponent {
  @Input() label: string = '';
  @Input() type: string = 'text';
  @Input() placeholder: string = '';
  @Input() icon: string | null | undefined = '';
  @Input() ngModel: any;

  @Input() variant: 'primary' | 'secondary' | 'transparent' = 'transparent';
  @Input() radius: 'default' | 'round' = 'default';

  @Input() value: any = '';
  @Output() valueChange = new EventEmitter<any>();

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.valueChange.emit(input.value);
  }
}
