import { HttpInterceptorFn } from '@angular/common/http';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = 'https://localhost:7047';
  const testTenantId = '8d1ed281-9f3b-4659-8a46-7eb26c5d550e';

  const isApiRequest = !req.url.startsWith('http');

  const finalUrl = isApiRequest
    ? `${baseUrl}${req.url.startsWith('/') ? req.url : '/' + req.url}`
    : req.url;

  const modifiedReq = req.clone({
    url: finalUrl,
    setHeaders: {
      'X-Tenant-Id': testTenantId,
      'Content-Type': 'application/json',
    },
  });

  return next(modifiedReq);
};
