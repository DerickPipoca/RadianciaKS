import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Component, EventEmitter, inject, Input, OnInit, Output } from '@angular/core';
import { CartService } from '../../../../core/services/cart-service';
import { OrderService } from '../../../../core/services/order-service';
import { PaymentMethod } from '../../../../core/enums/payment-method';
import { OrderItemRequestDto, OrderRequestDto } from '../../../../core/models/order.model';

@Component({
  selector: 'app-checkout-modal',
  imports: [CommonModule, FormsModule],
  templateUrl: './checkout-modal.html',
  styleUrl: './checkout-modal.scss',
})
export class CheckoutModal {
  @Output() close = new EventEmitter<void>();

  public cartService = inject(CartService);
  private orderService = inject(OrderService);

  public PaymentMethod = PaymentMethod;

  tableNumber: string = '';
  selectedMethod: PaymentMethod | null = null;
  amountReceived: number = 0;

  isProcessing: boolean = false;

  get total(): number {
    return this.cartService.subTotal();
  }

  get changeAmount(): number {
    const received = Number(this.amountReceived);
    if (this.selectedMethod !== PaymentMethod.Cash || this.amountReceived <= this.total) {
      return 0;
    }
    return this.amountReceived - this.total;
  }

  get isFormValid(): boolean {
    if (this.selectedMethod === null) return false;

    const received = Number(this.amountReceived);

    if (this.selectedMethod === PaymentMethod.Cash && received < this.total) {
      return false;
    }
    return true;
  }

  selectMethod(method: PaymentMethod): void {
    this.selectedMethod = method;

    if (method === PaymentMethod.Cash) {
      this.amountReceived = this.total;
    } else {
      this.amountReceived = 0;
    }
  }

  confirm(): void {
    if (!this.isFormValid) return;

    this.isProcessing = true;

    const orderItems: OrderItemRequestDto[] = this.cartService.items().map((cartItem) => ({
      productId: cartItem.product.id,
      quantity: cartItem.quantity,
      notes: cartItem.notes,
      selectedModifierIds: cartItem.selectedModifiers.map((modidifier) => modidifier.id),
    }));

    const payments = [
      {
        amount: this.total,
        method: this.selectedMethod!,
      },
    ];

    const payload: OrderRequestDto = {
      tableNumber: this.tableNumber ? this.tableNumber : undefined,
      items: orderItems,
      payments: payments,
    };

    this.orderService.create(payload).subscribe({
      next: (response) => {
        alert(`Venda finalizada com sucesso! (Pedido #${response.id.substring(0, 8)})`);
        this.cartService.clearCart();
        this.close.emit();
      },
      error: (err) => {
        console.error('Erro ao finalizar venda: ', err);
        this.isProcessing = false;
      },
    });
  }
}
