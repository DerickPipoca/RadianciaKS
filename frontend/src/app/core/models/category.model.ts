export interface CategoryResponseDto {
  id: string;
  name: string;
  priority?: number;
  imagePath?: string;
}

export interface CategoryRequestDto {
  name: string;
  imagePath?: string;
  priority?: number;
}
