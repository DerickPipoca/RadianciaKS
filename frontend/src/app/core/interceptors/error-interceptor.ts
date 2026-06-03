import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((httpError: HttpErrorResponse) => {
      let errorMessage = 'Ocorreu um erro inesperado ao comunicar com o servidor.';

      if (httpError.error && typeof httpError.error === 'object') {
        const errorData = httpError.error;

        if (errorData.details && Array.isArray(errorData.details)) {
          errorMessage = errorData.details.join('\n');
        } else if (errorData.error) {
          errorMessage = errorData.error;
        }
      } else if (httpError.status === 0) {
        errorMessage = 'Não foi possível ligar ao servidor. Verifique se a API está em execução.';
      }
      console.error('[Error Interceptor Capturado]:', errorMessage);
      alert(errorMessage);

      return throwError(() => new Error(errorMessage));
    }),
  );
};
