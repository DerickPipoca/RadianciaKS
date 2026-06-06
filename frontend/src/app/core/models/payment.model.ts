import { PaymentMethod } from '../enums/payment-method';

export interface PaymentRequestDto {
  amount: number;
  method: PaymentMethod;
}

export interface PaymentResponseDto {
  amount: number;
  method: PaymentMethod;
}
