import { CartService } from './../../../../core/services/cart-service';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OrderService } from '../../../../core/services/order-service';
import { OrderItemRequestDto, OrderRequestDto } from '../../../../core/models/order.model';
import { LucideAngularModule, ShoppingBagIcon, Trash2 } from 'lucide-angular';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart {
  public readonly ShoppingBag = ShoppingBagIcon;
  public readonly Trash = Trash2;

  public cartService = inject(CartService);
  private orderService = inject(OrderService);
  private router = inject(Router);
  isProcessing = false;

  removeItem(id: string) {
    this.cartService.removeItem(id);
  }
  clearCart(): void {
    if (confirm('Tem a certeza que deseja limpar todo o pedido?')) {
      this.cartService.clearCart();
    }
  }
  checkout(): void {
    console.log('A iniciar checkout com total de:', this.cartService.subTotal());
    this.router.navigate(['/pdv/checkout']);
  }
  closeOrder(): void {
    if (this.cartService.totalItems() === 0) return;

    this.isProcessing = true;

    const orderItems: OrderItemRequestDto[] = this.cartService.items().map((cartItem) => ({
      productId: cartItem.product.id,
      quantity: cartItem.quantity,
      notes: cartItem.notes,
      selectedModifierIds: cartItem.selectedModifiers.map((mod) => mod.id),
    }));

    // Cria o payload com o array de pagamentos VAZIO
    const payload: OrderRequestDto = {
      items: orderItems,
      payments: [],
    };

    this.orderService.create(payload).subscribe({
      next: (response) => {
        alert(`Pedido #${response.id.substring(0, 8)} enviado para a cozinha!`);
        this.cartService.clearCart();
        this.isProcessing = false;
      },
      error: (err) => {
        console.error('Erro ao lançar pedido: ', err);
        alert('Ocorreu um erro ao enviar o pedido.');
        this.isProcessing = false;
      },
    });
  }
}
