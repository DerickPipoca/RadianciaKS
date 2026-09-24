import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth-service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { LoginRequestDto, LoginResponseDto } from '../models/auth.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: { url: string; navigate: jasmine.Spy };

  // Utilitário para gerar tokens JWT válidos ou expirados sem dependências externas
  const createMockJwt = (offsetSeconds?: number, malformed = false): string => {
    if (malformed) return 'token-invalido-sem-pontos';

    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const payloadObj: Record<string, unknown> = { sub: 'user-123' };

    if (offsetSeconds !== undefined) {
      payloadObj['exp'] = Math.floor(Date.now() / 1000) + offsetSeconds;
    }

    const payload = btoa(JSON.stringify(payloadObj));
    return `${header}.${payload}.assinatura-falsa`;
  };

  beforeEach(() => {
    localStorage.clear();

    routerSpy = {
      url: '/orders',
      navigate: jasmine.createSpy('navigate'),
    };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerSpy },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    service = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('Validação do Token JWT e Sessão', () => {
    it('deve retornar falso quando não houver token no localStorage', () => {
      expect(service.isAuthenticated()).toBeFalse();
      expect(service.getToken()).toBeNull();
    });

    it('deve autenticar com sucesso quando o token tiver validade no futuro', () => {
      const validToken = createMockJwt(3600); // Expira em 1 hora
      localStorage.setItem('rk_token', validToken);

      expect(service.isAuthenticated()).toBeTrue();
      expect(service.getToken()).toBe(validToken);
    });

    it('deve rejeitar e deslogar quando o token já estiver expirado', () => {
      const expiredToken = createMockJwt(-60); // Expirado há 1 minuto
      localStorage.setItem('rk_token', expiredToken);
      localStorage.setItem('rk_user', JSON.stringify({ name: 'Operador', role: 'Cashier' }));

      const isValid = service.isAuthenticated();

      expect(isValid).toBeFalse();
      expect(localStorage.getItem('rk_token')).toBeNull();
      expect(localStorage.getItem('rk_user')).toBeNull();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('deve retornar falso e deslogar quando o token estiver malformado', () => {
      const malformedToken = createMockJwt(undefined, true);
      localStorage.setItem('rk_token', malformedToken);

      expect(service.isAuthenticated()).toBeFalse();
      expect(localStorage.getItem('rk_token')).toBeNull();
    });

    it('deve considerar válido se o token não contiver a claim exp', () => {
      const tokenWithoutExp = createMockJwt(undefined);
      localStorage.setItem('rk_token', tokenWithoutExp);

      expect(service.isAuthenticated()).toBeTrue();
    });
  });

  describe('Fluxo de Login', () => {
    it('deve realizar login, persistir dados no localStorage e atualizar o Signal currentUser', () => {
      const loginPayload: LoginRequestDto = {
        cpf: '12345678901',
        password: 'secretPassword',
      };

      const validToken = createMockJwt(7200);
      const mockResponse: LoginResponseDto = {
        token: validToken,
        name: 'Dérick Willian',
        role: 'Admin',
      };

      let loggedInStatus = false;
      service.isLoggedIn$.subscribe((status) => (loggedInStatus = status));

      service.login(loginPayload).subscribe((response) => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne('auth/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginPayload);
      req.flush(mockResponse);

      expect(localStorage.getItem('rk_token')).toBe(validToken);
      expect(localStorage.getItem('rk_user')).toBe(
        JSON.stringify({ name: 'Dérick Willian', role: 'Admin' }),
      );
      expect(service.currentUser()).toEqual({ name: 'Dérick Willian', role: 'Admin' });
      expect(loggedInStatus).toBeTrue();
    });
  });

  describe('Fluxo de Logout', () => {
    it('deve limpar dados, redefinir signals e navegar para /login se não estiver nela', () => {
      localStorage.setItem('rk_token', createMockJwt(3600));
      localStorage.setItem('rk_user', JSON.stringify({ name: 'Admin', role: 'Admin' }));
      service.currentUser.set({ name: 'Admin', role: 'Admin' });

      let currentLoggedInStatus: boolean | undefined;
      service.isLoggedIn$.subscribe((status) => (currentLoggedInStatus = status));

      routerSpy.url = '/pos/terminal';

      service.logout();

      expect(localStorage.getItem('rk_token')).toBeNull();
      expect(localStorage.getItem('rk_user')).toBeNull();
      expect(service.currentUser()).toBeNull();
      expect(currentLoggedInStatus).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('NÃO deve chamar router.navigate se a rota atual já for /login', () => {
      routerSpy.url = '/login';

      service.logout();

      expect(routerSpy.navigate).not.toHaveBeenCalled();
    });
  });

  describe('Recuperação de Usuário (getUser)', () => {
    it('deve recuperar os dados do usuário armazenados no localStorage', () => {
      const user = { name: 'Operador Caixa', role: 'Cashier' };
      localStorage.setItem('rk_user', JSON.stringify(user));

      const retrievedUser = service.getUser();

      expect(retrievedUser).toEqual(user);
    });

    it('deve retornar valores vazios quando rk_user não estiver no localStorage', () => {
      const emptyUser = service.getUser();

      expect(emptyUser).toEqual({ name: '', role: '' });
    });
  });
});
