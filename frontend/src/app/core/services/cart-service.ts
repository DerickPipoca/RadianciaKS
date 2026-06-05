import { computed, Injectable, signal } from '@angular/core';
import { CartItem } from '../models/cart-item.model';
import { ProductResponse } from '../models/product.model';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private cartItemsSignal = signal<CartItem[]>([]);

  public items = this.cartItemsSignal.asReadonly();

  public totalItems = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => acc + item.quantity, 0);
  });

  public subTotal = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => acc + item.totalPrice, 0);
  });

  public addProduct(product: ProductResponse, quantity: number = 1): void {
    const randomId = Math.random().toString(36).substring(2, 9);

    const newItem: CartItem = {
      id: randomId,
      product: product,
      quantity: quantity,
      selectedModifiers: [],
      unitPrice: product.price,
      totalPrice: product.price * quantity,
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
