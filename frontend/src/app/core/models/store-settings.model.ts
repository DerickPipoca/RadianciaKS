export interface StoreSettingsResponseDto {
  id: string;
  storeName: string;
  cnpj: string;
  address?: string;
  phone?: string;
  receiptFooter?: string;
  smallLogoPath?: string;
  bigLogoPath?: string;
  serviceCharge?: number;
}

export interface StoreSettingsRequestDto {
  storeName: string;
  cnpj: string;
  address?: string;
  phone?: string;
  receiptFooter?: string;
  smallLogoPath?: string;
  bigLogoPath?: string;
  serviceCharge?: number;
}
