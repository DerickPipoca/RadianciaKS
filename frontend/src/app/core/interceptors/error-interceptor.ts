import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError((httpError: HttpErrorResponse) => {
      let errorMessage = 'Ocorreu um erro inesperado ao comunicar com o servidor.';

      if (httpError.status === 0) {
        errorMessage =
          'Não foi possível conectar ao servidor. Verifique sua internet ou se a API está em execução.';
      } else if (httpError.error) {
        if (typeof httpError.error === 'string') {
          errorMessage = httpError.error;
        } else if (typeof httpError.error === 'object') {
          const errorData = httpError.error;

          if (errorData.details && Array.isArray(errorData.details)) {
            errorMessage = errorData.details.join('<br>');
          } else if (errorData.error) {
            errorMessage = errorData.error;
          } else if (errorData.message) {
            errorMessage = errorData.message;
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
