import { provideToastr, ToastrService } from 'ngx-toastr';
import { CartService } from './../../../../core/services/cart-service';
import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OrderService } from '../../../../core/services/order-service';
import { OrderItemRequestDto, OrderRequestDto } from '../../../../core/models/order.model';
import { LucideAngularModule, ShoppingBagIcon, Trash2 } from 'lucide-angular';
import { ButtonComponent } from '../../../../shared/components/button-component/button-component';
import { ModalComponent } from '../../../../shared/components/modal-component/modal-component';
import { OrderItemModifierResponseDto } from '../../../../core/models/modifier.model';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, FormsModule, LucideAngularModule, ButtonComponent, ModalComponent],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart implements OnDestroy {
  public readonly ShoppingBag = ShoppingBagIcon;
  public readonly Trash = Trash2;

  public cartService = inject(CartService);
  private orderService = inject(OrderService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  isProcessing = false;
  checkingOut = false;
  showCustomerModal = false;
  customerName = '';

  ngOnDestroy(): void {
    if (this.cartService.existingOrderData() !== null && this.checkingOut) {
      this.cartService.clearCart();
    }
  }

  removeItem(id?: string) {
    if (id) this.cartService.removeItem(id);
  }

  clearCart(): void {
    if (confirm('Tem a certeza que deseja limpar todo o pedido?')) {
      this.cartService.clearCart();
    }
  }

  checkout(): void {
    console.log('A iniciar checkout com total de:', this.cartService.subTotal());
    this.checkingOut = true;
    this.router.navigate(['/pdv/checkout']);
  }

  openCustomerModal(): void {
    if (this.cartService.totalItems() === 0) return;
    this.customerName = '';
    this.showCustomerModal = true;
  }

  closeCustomerModal(): void {
    this.showCustomerModal = false;
    this.customerName = '';
  }

  confirmOrder(): void {
    if (this.cartService.totalItems() === 0 || this.isProcessing) return;

    this.isProcessing = true;

    const orderItems: OrderItemRequestDto[] = this.cartService.items().map((cartItem) => ({
      productId: cartItem.product.id,
      quantity: cartItem.quantity,
      notes: cartItem.notes,
      promotionId: cartItem.promotionId || undefined,
      selectedModifierIds: cartItem.selectedModifiers.map((mod) => mod.id),
    }));

    const payload: OrderRequestDto = {
      items: orderItems,
      payments: [],
      tableNumber: this.customerName.trim() || undefined,
    };

    this.orderService.create(payload).subscribe({
      next: (response) => {
        this.toastr.success(
          `Pedido #${response.id.substring(0, 6).toUpperCase()} enviado para a cozinha!`,
        );
        this.cartService.clearCart();
        this.isProcessing = false;
        this.closeCustomerModal();
      },
      error: (err) => {
        console.error('Erro ao lançar pedido: ', err);
        this.toastr.error('Ocorreu um erro ao enviar o pedido.');
        this.isProcessing = false;
      },
    });
  }

  appendItemsToOrder() {
    this.isProcessing = true;
    const orderId = this.cartService.editingOrderId();
    const newItems = this.cartService.getNewItemsOnly();

    const payload: OrderItemRequestDto[] = newItems.map((item) => ({
      productId: item.product.id,
      quantity: item.quantity,
      notes: item.notes,
      promotionId: item.promotionId || undefined,
      selectedModifierIds: item.selectedModifiers.map((m) => m.id),
    }));

    this.orderService.addItemsToOrder(orderId!, payload).subscribe({
      next: () => {
        this.toastr.success('Novos itens adicionados ao pedido com sucesso!');
        this.cartService.clearCart();
        this.isProcessing = false;
        this.router.navigate(['/pdv/pedidos-aberto']);
      },
      error: () => {
        this.toastr.error('Falha ao adicionar novos itens à mesa.');
        this.isProcessing = false;
      },
    });
  }
  getGroupedModifiers(modifiers: OrderItemModifierResponseDto[]) {
    const groups: { [key: string]: OrderItemModifierResponseDto[] } = {};

    modifiers.forEach((mod) => {
      const groupName = mod.groupName || 'Adicionais';
      if (!groups[groupName]) {
        groups[groupName] = [];
      }
      groups[groupName].push(mod);
    });

    return Object.keys(groups).map((key) => ({
      groupName: key,
      options: groups[key],
    }));
  }
}
