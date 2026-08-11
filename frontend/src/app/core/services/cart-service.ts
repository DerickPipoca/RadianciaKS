import { OrderItemModifierResponseDto } from './../models/modifier.model';
import { computed, Injectable, signal } from '@angular/core';
import { CartItemDto } from '../models/cart-item.model';
import { ProductResponseDto } from '../models/product.model';
import { OrderResponseDto } from '../models/order.model';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private cartItemsSignal = signal<CartItemDto[]>([]);

  editingOrderId = signal<string | null>(null);
  existingOrderData = signal<OrderResponseDto | null>(null);

  public items = this.cartItemsSignal.asReadonly();

  public totalItems = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => acc + item.quantity, 0);
  });

  public subTotal = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => acc + item.totalPrice, 0);
  });

  public addProduct(
    product: ProductResponseDto,
    quantity: number = 1,
    selectedModifiers: OrderItemModifierResponseDto[] = [],
    notes?: string,
  ): void {
    const randomId = Math.random().toString(36).substring(2, 9);

    const modifiersTotal = selectedModifiers.reduce((sum, mod) => sum + mod.additionalPrice, 0);
    const unitPriceWithModifiers = product.price + modifiersTotal;
    const totalPrice = unitPriceWithModifiers * quantity;

    const newItem: CartItemDto = {
      id: randomId,
      product: product,
      quantity: quantity,
      selectedModifiers: selectedModifiers,
      notes: notes,
      unitPrice: unitPriceWithModifiers,
      totalPrice: totalPrice,
      isExistingItem: false,
    };

    this.cartItemsSignal.update((items) => [...items, newItem]);
  }

  public removeItem(id: string): void {
    this.cartItemsSignal.update((items) => items.filter((item) => item.id !== id));
  }

  public clearCart(): void {
    this.cartItemsSignal.set([]);
    this.editingOrderId.set(null);
    this.existingOrderData.set(null);
  }

  loadOrderForEditing(order: OrderResponseDto) {
    this.editingOrderId.set(order.id);
    this.existingOrderData.set(order);

    // Converte os itens antigos do pedido no formato do carrinho
    const pastItems: CartItemDto[] = order.items.map((item) => ({
      id: item.id,
      product: { id: item.productId, name: item.productName } as any,
      quantity: item.quantity,
      notes: item.notes,
      selectedModifiers: item.selectedModifiers || [],
      unitPrice: item.unitPrice,
      totalPrice: item.unitPrice * item.quantity,
      isExistingItem: true,
    }));

    this.cartItemsSignal.set(pastItems);
  }
  
  getNewItemsOnly(): CartItemDto[] {
    return this.cartItemsSignal().filter((item) => !item.isExistingItem);
  }
}
