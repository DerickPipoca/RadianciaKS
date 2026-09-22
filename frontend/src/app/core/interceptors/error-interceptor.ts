import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth-service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastr = inject(ToastrService);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((httpError: HttpErrorResponse) => {
      let errorMessage = 'Ocorreu um erro inesperado ao comunicar com o servidor.';
      const isLoginRequest = req.url.includes('/auth/login');

      if (httpError.status === 0) {
        errorMessage =
          'Não foi possível conectar ao servidor. Verifique se o sistema está em execução.';
      } else if (httpError.status === 401) {
        if (isLoginRequest) {
          // Erro de digitação na tela de login: não desloga, apenas avisa
          errorMessage = 'CPF ou senha incorretos.';
        } else {
          // Sessão caiu no meio do uso: limpa o storage e redireciona
          errorMessage = 'Sua sessão expirou. Faça login novamente para continuar.';
          authService.logout();
        }
      } else if (httpError.error) {
        if (typeof httpError.error === 'string') {
          errorMessage = httpError.error;
        } else if (typeof httpError.error === 'object') {
          const errorData = httpError.error;

          if (errorData.errors && typeof errorData.errors === 'object') {
            const validationMessages: string[] = [];

            for (const key in errorData.errors) {
              if (Object.prototype.hasOwnProperty.call(errorData.errors, key)) {
                validationMessages.push(...errorData.errors[key]);
              }
            }

            errorMessage = validationMessages.join('<br>');
          } else if (errorData.detail) {
            errorMessage = errorData.detail;
          } else if (errorData.title) {
            errorMessage = errorData.title;
          }
        }
      }

      console.error('[Error Interceptor Capturado]:', errorMessage);
      toastr.error(errorMessage, 'Atenção', { enableHtml: true });

      return throwError(() => new Error(errorMessage));
    }),
  );
};
