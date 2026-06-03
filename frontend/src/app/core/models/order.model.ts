import { OrderStatus } from '../enums/order-status';
import { KdsStatus } from './../enums/kds-status';
import { OrderItemModifierResponse } from './modifier.model';

export interface CheckoutRequest {
  payments: PaymentRequest[];
}

export interface OrderItemRequest {
  quantity: number;
  notes?: string;
  productId: string;

  selectedModifiersIds: string[];
}

export interface OrderItemResponse {
  id: string;
  productId: string;
  productName: string;
  notes?: string;
  quantity: number;
  unitPrice: number;
  kdsStatus: KdsStatus;
  selectedModifiers: OrderItemModifierResponse[];
}

export interface OrderRequestDto {
  tableNumber?: string;
  items: OrderItemRequest[];
  payments: PaymentRequest[];
}

export interface OrderResponseDto {
  id: string;
  tableNumber?: string;
  receiptUrl?: string;
  orderStatus: OrderStatus;
  totalAmount: number;
  changeAmount: number;

  items: OrderItemResponse[];
  payments: PaymentResponse[];
}
