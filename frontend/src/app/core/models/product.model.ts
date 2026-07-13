import { ModifierGroupResponseDto } from './modifier.model';

export interface ProductResponseDto {
  id: string;
  createdAt: Date;
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
