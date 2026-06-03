import { HttpInterceptorFn } from '@angular/common/http';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = 'https://localhost:7047/api';

  const testTenantId = '8d1ed281-9f3b-4659-8a46-7eb26c5d550e';

  if (req.url.startsWith('http') || req.url.startsWith('assets/')) {
    return next(req);
  }

  const cleanPath = req.url.startsWith('/') ? req.url.substring(1) : req.url;

  const apiReq = req.clone({
    url: `${baseUrl}/${cleanPath}`,
    setHeaders: {
      'X-Tenant-Id': testTenantId,
    },
  });

  return next(apiReq);
};
