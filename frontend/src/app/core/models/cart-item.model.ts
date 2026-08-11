import { OrderItemModifierResponseDto } from './modifier.model';
import { ProductResponseDto } from './product.model';

export interface CartItemDto {
  id?: string;
  product: ProductResponseDto;
  quantity: number;
  notes?: string;
  selectedModifiers: OrderItemModifierResponseDto[];
  unitPrice: number;
  totalPrice: number;
  isExistingItem?: boolean;
}
