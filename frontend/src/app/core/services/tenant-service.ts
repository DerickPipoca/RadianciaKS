import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environment/environment';
import { firstValueFrom, tap } from 'rxjs';
export interface TenantResponse {
  tenantId: string;
}
@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private http = inject(HttpClient);
  private currentTenantId = signal<string | null>(localStorage.getItem('tenant_id'));

  readonly tenantId = this.currentTenantId.asReadonly();

  async loadTenantConfig(): Promise<TenantResponse | void> {
    try {
      return await firstValueFrom(
        this.http.get<TenantResponse>('/config/tenant').pipe(
          tap((response) => {
            if (response?.tenantId) {
              this.currentTenantId.set(response.tenantId);
              localStorage.setItem('tenant_id', response.tenantId);
            }
          }),
        ),
      );
    } catch (error) {
      console.warn(
        '[Radiância KS] Falha ao recuperar Tenant do servidor. Usando cache local se disponível.',
        error,
      );
    }
  }

  getTenantId(): string | null {
    return this.currentTenantId();
  }
}
