import { OrderStatus } from '../enums/order-status';
import { KdsStatus } from './../enums/kds-status';
import { OrderItemModifierResponseDto } from './modifier.model';
import { PaymentRequestDto, PaymentResponseDto } from './payment.model';

export interface CheckoutRequestDto {
  payments: PaymentResponseDto[];
}

export interface OrderItemRequestDto {
  quantity: number;
  notes?: string;
  productId: string;

  selectedModifierIds: string[];
}

export interface OrderItemResponseDto {
  id: string;
  orderId: string;
  productId: string;
  productName: string;
  notes?: string;
  quantity: number;
  unitPrice: number;
  kdsStatus: KdsStatus;
  selectedModifiers: OrderItemModifierResponseDto[];
}

export interface OrderRequestDto {
  tableNumber?: string;
  items: OrderItemRequestDto[];
  payments: PaymentRequestDto[];
}

export interface OrderResponseDto {
  id: string;
  tableNumber?: string;
  receiptUrl?: string;
  orderStatus: OrderStatus;
  totalAmount: number;
  changeAmount: number;

  items: OrderItemResponseDto[];
  payments: PaymentResponseDto[];
}
