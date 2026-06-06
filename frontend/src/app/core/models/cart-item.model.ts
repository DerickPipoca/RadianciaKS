import { OrderItemModifierResponseDto } from './modifier.model';
import { ProductResponse } from './product.model';

export interface CartItemDto {
  id: string;
  product: ProductResponse;
  quantity: number;
  notes?: string;
  selectedModifiers: OrderItemModifierResponseDto[];
  unitPrice: number;
  totalPrice: number;
}
