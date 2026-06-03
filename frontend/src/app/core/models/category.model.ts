export interface CategoryResponse {
  id: string;
  name: string;
  priority?: number;
  imagePath?: string;
}

export interface CategoryRequest {
  name: string;
  imagePath?: string;
  priority?: number;
}
