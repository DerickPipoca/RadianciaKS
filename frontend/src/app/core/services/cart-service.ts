import { OrderItemModifierResponseDto } from './../models/modifier.model';
import { computed, Injectable, signal } from '@angular/core';
import { CartItemDto } from '../models/cart-item.model';
import { ProductResponse } from '../models/product.model';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private cartItemsSignal = signal<CartItemDto[]>([]);

  public items = this.cartItemsSignal.asReadonly();

  public totalItems = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => acc + item.quantity, 0);
  });

  public subTotal = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => acc + item.totalPrice, 0);
  });

  public addProduct(
    product: ProductResponse,
    quantity: number = 1,
    selectedModifiers: OrderItemModifierResponseDto[] = [],
    notes?: string,
  ): void {
    const randomId = Math.random().toString(36).substring(2, 9);

    const modifiersTotal = selectedModifiers.reduce((sum, mod) => sum + mod.additionalPrice, 0);
    const unitPricewithModifiers = product.price + modifiersTotal;
    const totalPrice = unitPricewithModifiers * quantity;

    const newItem: CartItemDto = {
      id: randomId,
      product: product,
      quantity: quantity,
      selectedModifiers: selectedModifiers,
      notes: notes,
      unitPrice: product.price,
      totalPrice: totalPrice,
    };

    this.cartItemsSignal.update((items) => [...items, newItem]);
  }

  public removeItem(id: string): void {
    this.cartItemsSignal.update((items) => items.filter((item) => item.id !== id));
  }

  public clearCart(): void {
    this.cartItemsSignal.set([]);
  }
}
