import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  HttpInterceptorFn,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';

import { errorInterceptor } from './error-interceptor';
import { AuthService } from '../services/auth-service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ToastrService } from 'ngx-toastr';

describe('errorInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    // 1. Cria os mocks dos serviços que o interceptor consome com inject()
    authServiceSpy = jasmine.createSpyObj('AuthService', ['logout']);
    toastrSpy = jasmine.createSpyObj('ToastrService', ['error']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Garante que não sobrou nenhuma chamada aberta
    httpMock.verify();
  });

  it('deve deslogar o usuário e exibir toast de sessão expirada no 401 em rota comum', () => {
    httpClient.get('/api/orders').subscribe({
      next: () => fail('A requisição deveria ter falhado com 401'),
      error: (error: Error) => {
        expect(error.message).toBe('Sua sessão expirou. Faça login novamente para continuar.');
      },
    });

    const req = httpMock.expectOne('/api/orders');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceSpy.logout).toHaveBeenCalledTimes(1);
    expect(toastrSpy.error).toHaveBeenCalledWith(
      'Sua sessão expirou. Faça login novamente para continuar.',
      'Atenção',
      { enableHtml: true },
    );
  });

  it('NÃO deve deslogar o usuário quando o 401 for na rota de login', () => {
    httpClient.post('/api/auth/login', { username: 'admin', password: '123' }).subscribe({
      next: () => fail('Deveria ter falhado com credenciais incorretas'),
      error: (error: Error) => {
        expect(error.message).toBe('CPF ou senha incorretos.');
      },
    });

    const req = httpMock.expectOne('/api/auth/login');
    req.flush('Invalid credentials', { status: 401, statusText: 'Unauthorized' });

    expect(authServiceSpy.logout).not.toHaveBeenCalled();
    expect(toastrSpy.error).toHaveBeenCalledWith('CPF ou senha incorretos.', 'Atenção', {
      enableHtml: true,
    });
  });

  it('deve tratar servidor offline (status 0)', () => {
    httpClient.get('/api/products').subscribe({
      next: () => fail('Deveria ter falhado com status 0'),
      error: (error: Error) => {
        expect(error.message).toBe(
          'Não foi possível conectar ao servidor. Verifique se o sistema está em execução.',
        );
      },
    });

    const req = httpMock.expectOne('/api/products');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(authServiceSpy.logout).not.toHaveBeenCalled();
    expect(toastrSpy.error).toHaveBeenCalledWith(
      'Não foi possível conectar ao servidor. Verifique se o sistema está em execução.',
      'Atenção',
      { enableHtml: true },
    );
  });

  it('deve concatenar mensagens de validação com quebra de linha quando houver dicionário de errors', () => {
    const errorPayload = {
      errors: {
        Name: ['Nome obrigatório.'],
        Price: ['Preço inválido.', 'Preço deve ser positivo.'],
      },
    };

    httpClient.post('/api/products', {}).subscribe({
      next: () => fail('Deveria ter falhado com validação'),
      error: (error: Error) => {
        expect(error.message).toBe(
          'Nome obrigatório.<br>Preço inválido.<br>Preço deve ser positivo.',
        );
      },
    });

    const req = httpMock.expectOne('/api/products');
    req.flush(errorPayload, { status: 400, statusText: 'Bad Request' });

    expect(toastrSpy.error).toHaveBeenCalledWith(
      'Nome obrigatório.<br>Preço inválido.<br>Preço deve ser positivo.',
      'Atenção',
      { enableHtml: true },
    );
  });
});
