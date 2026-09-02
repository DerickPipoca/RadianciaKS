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

  @Input() mask: 'cpf' | 'cnpj' | 'cpf-cnpj' | null = null;

  @Input() variant: 'primary' | 'secondary' | 'transparent' = 'transparent';
  @Input() fontSize: 'default' | 'big' = 'default';
  @Input() radius: 'default' | 'round' = 'default';
  @Input() disabled: boolean = false;

  @Input() value: any = '';
  @Output() valueChange = new EventEmitter<any>();

  formatValue(val: any): string {
    if (val === null || val === undefined) return '';

    if (this.formatDecimal) {
      return val.toString().replace('.', ',');
    }

    if (this.mask && typeof val === 'string') {
      return this.applyMask(val, this.mask);
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
    } else if (this.mask) {
      const cleanValue = val.replace(/\D/g, '');

      input.value = this.applyMask(cleanValue, this.mask);

      this.valueChange.emit(cleanValue);
    } else {
      this.valueChange.emit(val);
    }
  }

  private applyMask(val: string, maskType: string): string {
    let cleanValue = val.replace(/\D/g, '');

    if (maskType === 'cpf' || (maskType === 'cpf-cnpj' && cleanValue.length <= 11)) {
      cleanValue = cleanValue.substring(0, 11);
      cleanValue = cleanValue.replace(/(\d{3})(\d)/, '$1.$2');
      cleanValue = cleanValue.replace(/(\d{3})(\d)/, '$1.$2');
      cleanValue = cleanValue.replace(/(\d{3})(\d{1,2})$/, '$1-$2');
      return cleanValue;
    }

    if (maskType === 'cnpj' || (maskType === 'cpf-cnpj' && cleanValue.length > 11)) {
      cleanValue = cleanValue.substring(0, 14);
      cleanValue = cleanValue.replace(/(\d{2})(\d)/, '$1.$2');
      cleanValue = cleanValue.replace(/(\d{3})(\d)/, '$1.$2');
      cleanValue = cleanValue.replace(/(\d{3})(\d)/, '$1/$2');
      cleanValue = cleanValue.replace(/(\d{4})(\d{1,2})$/, '$1-$2');
      return cleanValue;
    }

    return val;
  }

  isLucideIcon(icon: string | LucideIconData | null | undefined): icon is LucideIconData {
    return typeof icon === 'object' && icon !== null;
  }
}
