import { OrderStatus } from '../enums/order-status';
import { PaymentStatus } from '../enums/payment-status';
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
  createdAt?: string | Date;
  selectedModifiers: OrderItemModifierResponseDto[];
}

export interface OrderRequestDto {
  tableNumber?: string;
  items: OrderItemRequestDto[];
  payments: PaymentRequestDto[];
}

export interface OrderResponseDto {
  id: string;
  createdAt: Date;
  tableNumber?: string;
  receiptUrl?: string;
  orderStatus: OrderStatus;
  totalAmount: number;
  changeAmount: number;
  paymentStatus: PaymentStatus;

  createdByName?: string;
  paidByName: string;

  items: OrderItemResponseDto[];
  payments: PaymentResponseDto[];
}

export interface KdsOrderGroup {
  orderId: string;
  items: OrderItemResponseDto[];
  status: KdsStatus;
  tableNumber: string;
  customerName: string;
  time: string;
  createdAt: Date;
}
