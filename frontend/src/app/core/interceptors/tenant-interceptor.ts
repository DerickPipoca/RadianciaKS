import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environment/environment';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant-service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);

  if (req.url.startsWith('assets/') || req.url.startsWith('/assets/')) {
    return next(req);
  }

  if (req.url.includes('/config/tenant')) {
    return next(req);
  }

  const activeTenantId = tenantService.getTenantId();

  // Injeta o cabeçalho se o tenant estiver definido
  const apiReq = req.clone({
    setHeaders: activeTenantId ? { 'X-Tenant-Id': activeTenantId } : {},
  });

  return next(apiReq);
};
