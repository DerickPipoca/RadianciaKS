import { OrderItemModifierResponse } from './modifier.model';
import { ProductResponse } from './product.model';

export interface CartItem {
  id: string;
  product: ProductResponse;
  quantity: number;
  notes?: string;
  selectedModifiers: OrderItemModifierResponse[];
  unitPrice: number;
  totalPrice: number;
}
