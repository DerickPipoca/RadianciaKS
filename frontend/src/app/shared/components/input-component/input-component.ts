import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideIconData, LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-input-component',
  imports: [FormsModule, LucideAngularModule],
  templateUrl: './input-component.html',
  styleUrl: './input-component.scss',
})
export class InputComponent {
  @Input() label: string = '';
  @Input() type: string = 'text';
  @Input() placeholder: string = '';
  @Input() icon: string | LucideIconData | null | undefined = '';
  @Input() ngModel: any;
  @Input() min: number | null | string = null;
  @Input() step: string | null = null;

  @Input() formatDecimal: boolean = false;

  @Input() variant: 'primary' | 'secondary' | 'transparent' = 'transparent';
  @Input() fontSize: 'default' | 'big' = 'default';
  @Input() radius: 'default' | 'round' = 'default';
  @Input() disabled: boolean = false;

  @Input() value: any = '';
  @Output() valueChange = new EventEmitter<any>();

  formatValue(val: any): string {
    if (this.formatDecimal && val !== null && val !== undefined) {
      return val.toString().replace('.', ',');
    }
    return val;
  }

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let val = input.value;

    if (this.formatDecimal) {
      val = val.replace(/\./g, ',');

      val = val.replace(/[^0-9,]/g, '');

      const parts = val.split(',');
      if (parts.length > 2) {
        val = parts[0] + ',' + parts.slice(1).join('');
      }

      input.value = val;

      const numericValue = val.replace(',', '.');
      this.valueChange.emit(numericValue);
    } else {
      this.valueChange.emit(val);
    }
  }

  isLucideIcon(icon: string | LucideIconData | null | undefined): icon is LucideIconData {
    return typeof icon === 'object' && icon !== null;
  }
}
