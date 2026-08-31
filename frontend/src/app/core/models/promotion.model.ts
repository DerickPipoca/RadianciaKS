import { ProductResponseDto } from './product.model';

export interface PromotionResponseDto {
  id: string;
  name: string;
  description: string;
  baseProduct: ProductResponseDto;
}

export interface PromotionModifierRequestDto {
  modifierOptionId: string;
  overridePrice: number;
}

export interface PromotionRequestDto {
  name: string;
  description: string;
  baseProductId: string;
  promotionalPrice: number | null;
  promotionModifiers: PromotionModifierRequestDto[];
}
