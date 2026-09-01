export interface ModifierGroupRequestDto {
  productId: string;
  name: string;
  minChoices: number;
  maxChoices: number;
  priority: number;
}

export interface ModifierGroupResponseDto {
  id: string;
  name: string;
  minChoices: number;
  maxChoices: number;
  priority: number;
  options: ModifierOptionResponseDto[];
}

export interface ModifierOptionRequestDto {
  name: string;
  additionalPrice: number;
  description?: string;
}

export interface ModifierOptionResponseDto {
  id: string;
  name: string;
  additionalPrice: number;
  imagePath?: string;
  description?: string;
  isPromotional?: boolean;
  promotionalPrice?: number;
}

export interface OrderItemModifierResponseDto {
  id: string;
  name: string;
  groupName: string;
  additionalPrice: number;
  originalAdditionalPrice?: number;
}
