import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environment/environment';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant-service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  const baseUrl = environment.apiUrl;

  if (req.url.startsWith('assets/')) {
    return next(req);
  }

  const cleanPath = req.url.startsWith('/') ? req.url.substring(1) : req.url;
  const targetUrl = req.url.startsWith('http') ? req.url : `${baseUrl}/${cleanPath}`;

  if (targetUrl.includes('/api/config/tenant')) {
    return next(req.clone({ url: targetUrl }));
  }

  const activeTenantId = tenantService.getTenantId();

  const apiReq = req.clone({
    url: targetUrl,
    setHeaders: activeTenantId ? { 'X-Tenant-Id': activeTenantId } : {},
  });

  return next(apiReq);
};
