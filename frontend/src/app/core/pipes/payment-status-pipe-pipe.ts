import { Pipe, PipeTransform } from '@angular/core';
import { PaymentStatus } from '../enums/payment-status';

@Pipe({ name: 'paymentStatusLabel', standalone: true })
export class PaymentStatusLabelPipe implements PipeTransform {
  transform(status: PaymentStatus | string | number): string {
    const parsedStatus =
      typeof status === 'string' ? PaymentStatus[status as keyof typeof PaymentStatus] : status;

    switch (parsedStatus) {
      case PaymentStatus.Pending:
        return 'Pendente';
      case PaymentStatus.Partial:
        return 'Pago Parcial';
      case PaymentStatus.Paid:
        return 'Pago';
      case PaymentStatus.Refunded:
        return 'Estornado';
      default:
        return 'Desconhecido';
    }
  }
}

@Pipe({ name: 'paymentStatusClass', standalone: true })
export class PaymentStatusClassPipe implements PipeTransform {
  transform(status: PaymentStatus | string | number): string {
    const parsedStatus =
      typeof status === 'string' ? PaymentStatus[status as keyof typeof PaymentStatus] : status;

    switch (parsedStatus) {
      case PaymentStatus.Pending:
        return 'pending';
      case PaymentStatus.Partial:
        return 'partial';
      case PaymentStatus.Paid:
        return 'paid';
      case PaymentStatus.Refunded:
        return 'refunded';
      default:
        return 'pending';
    }
  }
}
