import { CartService } from './../../../../core/services/cart-service';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { CheckoutModal } from '../../components/checkout-modal/checkout-modal';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, CheckoutModal],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart {
  public cartService = inject(CartService);

  showCheckoutModal = false;

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
    this.showCheckoutModal = true;
  }
}
