export interface ModifierGroupRequest {
  productId: string;
  name: string;
  minChoices: number;
  maxChoices: number;
}

export interface ModifierGroupResponse {
  id: string;
  name: string;
  minChoices: number;
  maxChoices: number;
  options: ModifierOptionResponse[];
}

export interface ModifierOptionRequest {
  name: string;
  additionalPrice: number;
}

export interface ModifierOptionResponse {
  id: string;
  name: string;
  additionalPrice: number;
}

export interface OrderItemModifierResponse {
  id: string;
  name: string;
  additionalPrice: number;
}
