import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, OnInit, Output } from '@angular/core';
import { ProductResponseDto } from '../../../../core/models/product.model';
import { CartService } from '../../../../core/services/cart-service';
import {
  ModifierGroupResponseDto,
  ModifierOptionResponseDto,
  OrderItemModifierResponseDto,
} from '../../../../core/models/modifier.model';
import { FormsModule } from '@angular/forms';
import Decimal from 'decimal.js';

@Component({
  selector: 'app-modifier-modal',
  imports: [CommonModule, FormsModule],
  templateUrl: './modifier-modal.html',
  styleUrl: './modifier-modal.scss',
})
export class ModifierModal implements OnInit {
  @Input({ required: true }) product!: ProductResponseDto;
  @Input() promotionId: string | null = null;

  @Output() close = new EventEmitter<void>();

  private cartService = inject(CartService);

  quantity: number = 1;
  notes: string = '';

  selections = new Map<
    string,
    { group: ModifierGroupResponseDto; options: ModifierOptionResponseDto[] }
  >();

  ngOnInit(): void {
    if (this.product && this.product.modifierGroups) {
      this.product.modifierGroups.forEach((group) => {
        this.selections.set(group.id, { group: group, options: [] });
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

  getOptionCount(groupId: string, optionId: string): number {
    const currentData = this.selections.get(groupId);
    if (!currentData) return 0;
    return currentData.options.filter((o) => o.id === optionId).length;
  }

  addOption(group: ModifierGroupResponseDto, option: ModifierOptionResponseDto) {
    const currentData = this.selections.get(group.id);
    if (!currentData) return;

    const currentSelections = currentData.options;

    if (group.maxChoices === 1) {
      this.selections.set(group.id, { group: group, options: [option] });
    } else {
      if (currentSelections.length < group.maxChoices) {
        this.selections.set(group.id, { group: group, options: [...currentSelections, option] });
      }
    }
  }

  removeOption(group: ModifierGroupResponseDto, option: ModifierOptionResponseDto) {
    const currentData = this.selections.get(group.id);
    if (!currentData) return;

    const currentSelections = currentData.options;
    const index = currentSelections.findIndex((o) => o.id === option.id);

    if (index !== -1) {
      const newSelections = [...currentSelections];
      newSelections.splice(index, 1);
      this.selections.set(group.id, { group: group, options: newSelections });
    }
  }

  isGroupValid(group: ModifierGroupResponseDto): boolean {
    const currentData = this.selections.get(group.id);
    if (!currentData) return false;
    const currentSelections = currentData.options;
    return (
      currentSelections.length >= group.minChoices && currentSelections.length <= group.maxChoices
    );
  }

  get isFormValid(): boolean {
    if (!this.product.modifierGroups) return true;
    return this.product.modifierGroups.every((group) => this.isGroupValid(group));
  }

  get totalPrice(): number {
    let modifiersTotal = new Decimal(0);

    this.selections.forEach((data) => {
      data.options.forEach((opt) => {
        const optPrice =
          opt.isPromotional && opt.promotionalPrice !== undefined
            ? opt.promotionalPrice
            : opt.additionalPrice;

        modifiersTotal = modifiersTotal.plus(new Decimal(optPrice));
      });
    });

    const basePriceValue =
      this.product.isPromotional && this.product.promotionalPrice !== undefined
        ? this.product.promotionalPrice
        : this.product.price;

    const basePrice = new Decimal(basePriceValue);
    const finalUnit = basePrice.plus(modifiersTotal);

    return finalUnit.times(this.quantity).toNumber();
  }

  confirm() {
    if (!this.isFormValid) return;

    const selectedModifiers: OrderItemModifierResponseDto[] = [];

    this.selections.forEach((data) => {
      data.options.forEach((opt) => {
        const finalPrice =
          opt.isPromotional && opt.promotionalPrice !== undefined
            ? opt.promotionalPrice
            : opt.additionalPrice;

        selectedModifiers.push({
          id: opt.id,
          name: opt.name,
          additionalPrice: finalPrice,
          groupName: data.group.name,
        });
      });
    });

    this.cartService.addProduct(
      this.product,
      this.quantity,
      selectedModifiers,
      this.notes,
      this.promotionId,
    );
    this.close.emit();
  }
}
