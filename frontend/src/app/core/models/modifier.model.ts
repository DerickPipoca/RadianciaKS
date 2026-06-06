export interface ModifierGroupRequestDto {
  productId: string;
  name: string;
  minChoices: number;
  maxChoices: number;
}

export interface ModifierGroupResponseDto {
  id: string;
  name: string;
  minChoices: number;
  maxChoices: number;
  options: ModifierOptionResponseDto[];
}

export interface ModifierOptionRequestDto {
  name: string;
  additionalPrice: number;
}

export interface ModifierOptionResponseDto {
  id: string;
  name: string;
  additionalPrice: number;
}

export interface OrderItemModifierResponseDto {
  id: string;
  name: string;
  additionalPrice: number;
}
