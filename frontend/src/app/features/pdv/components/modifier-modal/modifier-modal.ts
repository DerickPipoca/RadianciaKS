import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, OnInit, Output } from '@angular/core';
import { ProductResponse } from '../../../../core/models/product.model';
import { CartService } from '../../../../core/services/cart-service';
import {
  ModifierGroupResponseDto,
  ModifierOptionResponseDto,
  OrderItemModifierResponseDto,
} from '../../../../core/models/modifier.model';

@Component({
  selector: 'app-modifier-modal',
  imports: [CommonModule],
  templateUrl: './modifier-modal.html',
  styleUrl: './modifier-modal.scss',
})
export class ModifierModal implements OnInit {
  @Input({ required: true }) product!: ProductResponse;

  @Output() close = new EventEmitter<void>();

  private cartService = inject(CartService);

  quantity = 1;
  selections = new Map<string, ModifierOptionResponseDto[]>();

  ngOnInit(): void {
    if (this.product && this.product.modifierGroups) {
      this.product.modifierGroups.forEach((group) => {
        this.selections.set(group.id, []);
      });
    }
  }

  increaseQty() {
    this.quantity++;
  }

  decreaseQty() {
    if (this.quantity > 1) {
      this.quantity--;
    } else {
      this.close.emit();
    }
  }

  toggleOption(group: ModifierGroupResponseDto, option: ModifierOptionResponseDto) {
    const currentSelections = this.selections.get(group.id) || [];
    const isAlreadySelected = currentSelections.some((o) => o.id === option.id);
    if (group.maxChoices === 1) {
      this.selections.set(group.id, [option]);
    } else {
      if (isAlreadySelected) {
        this.selections.set(
          group.id,
          currentSelections.filter((o) => o.id !== option.id),
        );
      } else {
        if (currentSelections.length < group.maxChoices) {
          this.selections.set(group.id, [...currentSelections, option]);
        }
      }
    }
  }

  isOptionSelected(groupId: string, optionId: string): boolean {
    const currentSelections = this.selections.get(groupId) || [];
    return currentSelections.some((o) => o.id === optionId);
  }

  isGroupValid(group: ModifierGroupResponseDto): boolean {
    const currentSelections = this.selections.get(group.id) || [];
    return (
      currentSelections.length >= group.minChoices && currentSelections.length <= group.maxChoices
    );
  }

  get isFormValid(): boolean {
    if (!this.product.modifierGroups) return true;
    return this.product.modifierGroups.every((group) => this.isGroupValid(group));
  }

  get totalPrice(): number {
    let modifiersTotal = 0;
    this.selections.forEach((options) => {
      options.forEach((opt) => (modifiersTotal += opt.additionalPrice));
    });
    return (this.product.price + modifiersTotal) * this.quantity;
  }

  confirm() {
    if (!this.isFormValid) return;

    const selectedModifiers: OrderItemModifierResponseDto[] = [];
    this.selections.forEach((options) => {
      options.forEach((opt) => {
        selectedModifiers.push({
          id: opt.id,
          name: opt.name,
          additionalPrice: opt.additionalPrice,
        });
      });
    });
    this.cartService.addProduct(this.product, this.quantity, selectedModifiers);
    this.close.emit();
  }
}
