import { ModifierGroupResponseDto } from './modifier.model';

export interface ProductResponse {
  id: string;
  name: string;
  description?: string;
  price: number;
  imagePath?: string;
  categoryId: string;
  categoryName: string;
  modifierGroups: ModifierGroupResponseDto[];
}

export interface ProductRequestDto {
  name: string;
  description?: string;
  price: number;
  imagePath?: string;
  categoryId: string;
}
