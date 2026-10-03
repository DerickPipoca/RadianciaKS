import { inject, Injectable, signal } from '@angular/core';
import { Network, ConnectionType } from '@capacitor/network';
import { environment } from '../../../environment/environment';
import { TenantService } from './tenant-service';

export type ActiveRoute = 'local' | 'cloud';

@Injectable({
  providedIn: 'root',
})
export class EndpointService {
  private tenantService = inject(TenantService);
  public readonly localBase = 'http://192.168.0.67:8080/api';

  public get cloudBase(): string {
    return this.tenantService.getCloudApiUrl();
  }

  public activeRoute = signal<ActiveRoute>('local');
  public activeBaseUrl = signal<string>(this.localBase);

  constructor() {
    this.initNetworkListener();
  }

  private async initNetworkListener(): Promise<void> {
    try {
      const status = await Network.getStatus();
      this.handleConnectionChange(status.connectionType);

      Network.addListener('networkStatusChange', (status) => {
        this.handleConnectionChange(status.connectionType);
      });
    } catch {
      this.switchToLocal();
    }
  }

  private handleConnectionChange(connectionType: ConnectionType): void {
    if (connectionType === 'cellular') {
      this.switchToCloud();
    } else if (connectionType === 'wifi') {
      this.checkLocalAvailability();
    }
  }

  public switchToCloud(): void {
    if (this.activeBaseUrl() !== this.cloudBase) {
      console.warn(`[Failover] Comutando para a nuvem: ${this.cloudBase}`);
      this.activeBaseUrl.set(this.cloudBase);
    }
  }

  public switchToLocal(): void {
    if (this.activeBaseUrl() !== this.localBase) {
      console.log(`[Failover] Restaurando rede local: ${this.localBase}`);
      this.activeBaseUrl.set(this.localBase);
    }
  }

  public isUsingLocal(): boolean {
    return this.activeRoute() === 'local';
  }

  public rewriteUrl(url: string, targetBase: string = this.activeBaseUrl()): string {
    if (url.startsWith('assets/') || url.startsWith('/assets/')) {
      return url;
    }

    const apiIndex = url.indexOf('/api');
    if (apiIndex !== -1) {
      const pathAfterApi = url.substring(apiIndex + 4);
      const cleanPath = pathAfterApi.startsWith('/') ? pathAfterApi : `/${pathAfterApi}`;
      return `${targetBase}${cleanPath}`;
    }

    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }

    const cleanPath = url.startsWith('/') ? url : `/${url}`;
    return `${targetBase}${cleanPath}`;
  }

  public async checkLocalAvailability(): Promise<boolean> {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 1500);

      const response = await fetch(`${this.localBase}/StoreSettings`, {
        method: 'GET',
        signal: controller.signal,
      });

      clearTimeout(timeoutId);
      if (response.ok) {
        this.switchToLocal();
        return true;
      }
    } catch {}

    this.switchToCloud();
    return false;
  }
}
