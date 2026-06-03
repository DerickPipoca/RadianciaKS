import { PaymentMethod } from '../enums/payment-method';

export interface PaymentRequest {
  amount: number;
  method: PaymentMethod;
}

export interface PaymentResponse {
  amount: number;
  method: PaymentMethod;
}
