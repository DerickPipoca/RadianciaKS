import { OrderItemModifierResponseDto } from './../models/modifier.model';
import { computed, Injectable, signal } from '@angular/core';
import { CartItemDto } from '../models/cart-item.model';
import { ProductResponseDto } from '../models/product.model';
import { OrderResponseDto } from '../models/order.model';
import Decimal from 'decimal.js';

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

  subTotal = computed(() => {
    return this.cartItemsSignal().reduce((acc, item) => {
      return acc.plus(new Decimal(item.totalPrice || 0));
    }, new Decimal(0));
  });

  public addProduct(
    product: ProductResponseDto,
    quantity: number = 1,
    selectedModifiers: OrderItemModifierResponseDto[] = [],
    notes?: string,
    promotionId?: string | null,
  ): void {
    const existingItemIndex = this.cartItemsSignal().findIndex((item) => {
      if (item.product.id !== product.id || item.isExistingItem) return false;
      if (item.notes !== notes) return false;

      if (item.promotionId !== promotionId) return false;

      const mods1 = item.selectedModifiers || [];
      const mods2 = selectedModifiers || [];
      if (mods1.length !== mods2.length) return false;

      return mods1.every((m1) => mods2.some((m2) => m1.id === m2.id));
    });

    const basePrice =
      product.isPromotional && product.promotionalPrice !== undefined
        ? new Decimal(product.promotionalPrice)
        : new Decimal(product.price);

    const modifiersTotal = selectedModifiers.reduce(
      (sum, mod) => sum.plus(mod.additionalPrice),
      new Decimal(0),
    );

    const unitPriceWithModifiers = basePrice.plus(modifiersTotal);

    if (existingItemIndex > -1) {
      this.cartItemsSignal.update((items) => {
        const newItems = [...items];
        const item = newItems[existingItemIndex];

        item.quantity += quantity;
        item.totalPrice = unitPriceWithModifiers.times(item.quantity).toNumber();

        return newItems;
      });
    } else {
      const randomId = Math.random().toString(36).substring(2, 9);
      const totalPrice = unitPriceWithModifiers.times(quantity);

      const newItem: CartItemDto = {
        id: randomId,
        product: product,
        quantity: quantity,
        selectedModifiers: selectedModifiers,
        notes: notes,
        unitPrice: unitPriceWithModifiers.toNumber(),
        totalPrice: totalPrice.toNumber(),
        isExistingItem: false,
        promotionId: promotionId,
      };

      this.cartItemsSignal.update((items) => [...items, newItem]);
    }
  }

  public decrementQuantity(id?: string): void {
    if (!id) return;
    this.cartItemsSignal.update((items) => {
      const index = items.findIndex((item) => item.id === id);
      if (index === -1) return items;

      const newItems = [...items];
      const item = newItems[index];

      if (item.quantity > 1) {
        item.quantity -= 1;

        const basePrice =
          item.product.isPromotional && item.product.promotionalPrice !== undefined
            ? new Decimal(item.product.promotionalPrice)
            : new Decimal(item.product.price);

        const modsTotal = (item.selectedModifiers || []).reduce(
          (acc, mod) => acc.plus(mod.additionalPrice),
          new Decimal(0),
        );

        const unitPrice = basePrice.plus(modsTotal);
        item.totalPrice = unitPrice.times(item.quantity).toNumber();
      } else {
        newItems.splice(index, 1);
      }

      return newItems;
    });
  }

  public incrementQuantity(id?: string): void {
    if (!id) return;
    this.cartItemsSignal.update((items) => {
      const index = items.findIndex((item) => item.id === id);
      if (index === -1) return items;

      const newItems = [...items];
      const item = newItems[index];

      item.quantity += 1;

      const basePrice =
        item.product.isPromotional && item.product.promotionalPrice !== undefined
          ? new Decimal(item.product.promotionalPrice)
          : new Decimal(item.product.price);

      const modsTotal = (item.selectedModifiers || []).reduce(
        (acc, mod) => acc.plus(mod.additionalPrice),
        new Decimal(0),
      );

      const unitPrice = basePrice.plus(modsTotal);
      item.totalPrice = unitPrice.times(item.quantity).toNumber();

      return newItems;
    });
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
