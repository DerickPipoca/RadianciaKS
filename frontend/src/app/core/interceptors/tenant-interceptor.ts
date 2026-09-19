import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environment/environment';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant-service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);

  if (req.url.startsWith('assets/') || req.url.startsWith('/assets/')) {
    return next(req);
  }

  let targetUrl = req.url;

  if (!targetUrl.startsWith('http://') && !targetUrl.startsWith('https://')) {
    const pathWithSlash = targetUrl.startsWith('/') ? targetUrl : `/${targetUrl}`;

    if (pathWithSlash.startsWith('/api')) {
      targetUrl = pathWithSlash;
    } else {
      targetUrl = `/api${pathWithSlash}`;
    }
  }

  if (targetUrl.includes('/config/tenant')) {
    return next(req.clone({ url: targetUrl }));
  }

  const activeTenantId = tenantService.getTenantId();

  const apiReq = req.clone({
    url: targetUrl,
    setHeaders: activeTenantId ? { 'X-Tenant-Id': activeTenantId } : {},
  });

  return next(apiReq);
};
