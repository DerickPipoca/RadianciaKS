export interface LoginRequestDto {
  cpf: string;
  password?: string;
}

export interface LoginResponseDto {
  token: string;
  name: string;
  role: string;
}
