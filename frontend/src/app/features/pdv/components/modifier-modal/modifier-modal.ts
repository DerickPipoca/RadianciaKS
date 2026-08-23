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

  @Output() close = new EventEmitter<void>();

  private cartService = inject(CartService);

  quantity: number = 1;
  notes: string = '';

  // Alteramos o Map para guardar também a referência do grupo inteiro, não apenas as opções
  // Isso facilita buscar o nome do grupo na hora de confirmar
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

  toggleOption(group: ModifierGroupResponseDto, option: ModifierOptionResponseDto) {
    const currentData = this.selections.get(group.id);
    if (!currentData) return;

    const currentSelections = currentData.options;
    const isAlreadySelected = currentSelections.some((o) => o.id === option.id);

    if (group.maxChoices === 1) {
      this.selections.set(group.id, { group: group, options: [option] });
    } else {
      if (isAlreadySelected) {
        this.selections.set(group.id, {
          group: group,
          options: currentSelections.filter((o) => o.id !== option.id),
        });
      } else {
        if (currentSelections.length < group.maxChoices) {
          this.selections.set(group.id, { group: group, options: [...currentSelections, option] });
        }
      }
    }
  }

  isOptionSelected(groupId: string, optionId: string): boolean {
    const currentData = this.selections.get(groupId);
    if (!currentData) return false;
    return currentData.options.some((o) => o.id === optionId);
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
        modifiersTotal = modifiersTotal.plus(new Decimal(opt.additionalPrice));
      });
    });

    const basePrice = new Decimal(this.product.price);
    const finalUnit = basePrice.plus(modifiersTotal);

    return finalUnit.times(this.quantity).toNumber();
  }

  confirm() {
    if (!this.isFormValid) return;

    const selectedModifiers: OrderItemModifierResponseDto[] = [];

    this.selections.forEach((data) => {
      data.options.forEach((opt) => {
        selectedModifiers.push({
          id: opt.id,
          name: opt.name,
          additionalPrice: opt.additionalPrice,
          groupName: data.group.name,
        });
      });
    });

    this.cartService.addProduct(this.product, this.quantity, selectedModifiers, this.notes);
    this.close.emit();
  }
}
