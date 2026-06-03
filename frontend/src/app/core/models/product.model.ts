import { ModifierGroupResponse } from './modifier.model';

export interface ProductResponse {
  id: string;
  name: string;
  description?: string;
  price: number;
  imagePath?: string;
  categoryId: string;
  categoryName: string;
  modifierGroups: ModifierGroupResponse[];
}

export interface ProductRequest {
  name: string;
  description?: string;
  price: number;
  imagePath?: string;
  categoryId: string;
}
