import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { timeout, catchError, throwError, TimeoutError } from 'rxjs';
import { EndpointService } from '../services/endpoint-service';
import { environment } from '../../../environment/environment';

export const failoverInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  const endpointService = inject(EndpointService);

  // Aplica o endpoint base ativo no momento
  const activeUrl = endpointService.rewriteUrl(req.url);
  const adjustedReq = req.clone({ url: activeUrl });

  // Apenas chamadas na rota local precisam de timeout agressivo para failover rápido
  if (endpointService.isUsingLocal()) {
    return next(adjustedReq).pipe(
      timeout(1500),
      catchError((error: unknown) => {
        const isNetworkFailure = error instanceof HttpErrorResponse && error.status === 0;
        const isTimeout = error instanceof TimeoutError;

        if (isNetworkFailure || isTimeout) {
          // Comuta imediatamente para o Cloudflare Tunnel
          endpointService.switchToCloud();

          const fallbackUrl = endpointService.rewriteUrl(
            req.url,
            environment.cloudApiUrl.replace(/\/+$/, ''),
          );
          const cloudReq = req.clone({ url: fallbackUrl });

          return next(cloudReq);
        }

        return throwError(() => error);
      }),
    );
  }

  return next(adjustedReq);
};
