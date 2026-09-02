import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../../../core/services/cart-service';
import { OrderService } from '../../../../core/services/order-service';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentMethod } from '../../../../core/enums/payment-method';
import { PaymentRequestDto } from '../../../../core/models/payment.model';
import {
  CheckoutRequestDto,
  OrderItemRequestDto,
  OrderRequestDto,
  OrderResponseDto,
} from '../../../../core/models/order.model';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { ToastrService } from 'ngx-toastr';
import Decimal from 'decimal.js';
import { InputComponent } from '../../../../shared/components/input-component/input-component';

@Component({
  selector: 'app-checkout',
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout implements OnInit {
  public cartService = inject(CartService);
  private orderService = inject(OrderService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);

  public PaymentMethod = PaymentMethod;

  existingOrderId: string | null = null;
  existingOrderTotal: number = 0;
  alreadyPaidAmount: number = 0;

  tableNumber: string = '';
  isProcessing: boolean = false;

  abertos = false;

  addedPayments: PaymentRequestDto[] = [];
  selectedMethod: PaymentMethod = PaymentMethod.Pix;
  amountToAdd: number = 0;

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      const orderId = params['orderId'];
      const abertos = params['abertos'];

      if (params['abertos']) {
        this.abertos = true;
      }

      if (params['orderId']) {
        this.existingOrderId = orderId;
        this.loadExistingOrder(orderId);
      } else {
        if (this.cartService.totalItems() === 0) {
          this.goBack();
        } else {
          this.amountToAdd = this.remainingAmount;
        }
      }
    });
  }

  loadExistingOrder(id: string) {
    this.isProcessing = true;
    this.orderService.getById(id).subscribe({
      next: (order: OrderResponseDto) => {
        this.existingOrderTotal = order.totalAmount;

        this.tableNumber = order.tableNumber || '';

        if (order.payments && order.payments.length > 0) {
          this.alreadyPaidAmount = order.payments.reduce((acc, p) => acc + p.amount, 0);
        } else {
          this.alreadyPaidAmount = 0;
        }

        this.amountToAdd = this.remainingAmount;
        this.isProcessing = false;
      },
      error: (err) => {
        console.error('Erro ao carregar pedido existente:', err);
        this.toastr.error('Pedido não encontrado.');
        this.goBack();
      },
    });
  }

  get total(): number {
    return this.existingOrderId ? this.existingOrderTotal : this.cartService.subTotal().toNumber();
  }

  get totalPaid(): number {
    const sumNew = this.addedPayments.reduce((acc, p) => acc.plus(p.amount), new Decimal(0));

    const total = sumNew.plus(this.alreadyPaidAmount);

    return total.toNumber();
  }

  get remainingAmount(): number {
    const totalDec = new Decimal(this.total);
    const paidDec = new Decimal(this.totalPaid);

    const rem = totalDec.minus(paidDec);

    return rem.toNumber() > 0 ? rem.toNumber() : 0;
  }

  get changeAmount(): number {
    if (!this.totalPaid || !this.total) return 0;

    const change = new Decimal(this.totalPaid).minus(this.total);

    return change.toNumber();
  }

  get isFormValid(): boolean {
    return this.totalPaid >= this.total && this.total > 0;
  }

  getPaymentMethodName(method: PaymentMethod): string {
    const names: Record<number, string> = {
      [PaymentMethod.Cash]: 'Dinheiro',
      [PaymentMethod.CreditCard]: 'Crédito',
      [PaymentMethod.DebitCard]: 'Débito',
      [PaymentMethod.Pix]: 'PIX',
      [PaymentMethod.Other]: 'Outro',
    };
    return names[method] || 'Desconhecido';
  }

  selectMethod(method: PaymentMethod): void {
    this.selectedMethod = method;
    this.amountToAdd = this.remainingAmount;
  }

  addPayment(): void {
    if (this.amountToAdd <= 0) return;

    if (this.selectedMethod !== PaymentMethod.Cash && this.amountToAdd > this.remainingAmount) {
      this.amountToAdd = this.remainingAmount;
    }

    this.addedPayments.push({
      method: this.selectedMethod,
      amount: this.amountToAdd,
    });

    this.amountToAdd = this.remainingAmount;
  }

  removePayment(index: number): void {
    this.addedPayments.splice(index, 1);
    this.amountToAdd = this.remainingAmount;
  }

  goBack(): void {
    if (this.existingOrderId) {
      if (this.abertos) {
        this.router.navigate(['/pdv/pedidos-aberto']);
      } else {
        this.router.navigate(['/pdv/pedidos']);
      }
    } else {
      this.router.navigate(['/pdv/catalogo']);
    }
  }

  confirm(): void {
    if (!this.isFormValid) return;
    this.isProcessing = true;

    if (this.existingOrderId) {
      const paymentPayload = {
        payments: this.addedPayments,
      };

      const checkoutDto: CheckoutRequestDto = { payments: this.addedPayments };

      this.orderService.checkoutOrder(this.existingOrderId, checkoutDto).subscribe({
        next: () => {
          this.toastr.success(
            `Pagamento do Pedido #${this.existingOrderId!.substring(0, 6).toUpperCase()} realizado!`,
          );
          this.router.navigate(['/pdv/pedidos']);
        },
        error: (err) => {
          this.toastr.error('Ocorreu um erro ao processar o pagamento.');
          this.isProcessing = false;
        },
      });
    } else {
      const orderItems: OrderItemRequestDto[] = this.cartService.items().map((cartItem) => ({
        productId: cartItem.product.id,
        quantity: cartItem.quantity,
        notes: cartItem.notes,
        promotionId: cartItem.promotionId || undefined,
        selectedModifierIds: cartItem.selectedModifiers.map((mod) => mod.id),
      }));

      const payload: OrderRequestDto = {
        tableNumber: this.tableNumber ? this.tableNumber : undefined,
        items: orderItems,
        payments: this.addedPayments,
      };

      this.orderService.create(payload).subscribe({
        next: (response) => {
          this.toastr.success(
            `Venda finalizada com sucesso! (Pedido #${response.id.substring(0, 6).toUpperCase()})`,
          );
          this.cartService.clearCart();
          this.router.navigate(['/pdv']);
        },
        error: (err) => {
          console.error('Erro ao finalizar venda: ', err);
          this.toastr.error('Ocorreu um erro ao finalizar a venda.');
          this.isProcessing = false;
        },
      });
    }
  }
}
