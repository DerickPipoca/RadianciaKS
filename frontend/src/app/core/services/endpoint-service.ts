import { Injectable, signal } from '@angular/core';
import { Network, ConnectionType } from '@capacitor/network';
import { environment } from '../../../environment/environment';

export type ActiveRoute = 'local' | 'cloud';

@Injectable({
  providedIn: 'root',
})
export class EndpointService {
  private readonly localBase = environment.localApiUrl.replace(/\/+$/, '');
  private readonly cloudBase = environment.cloudApiUrl.replace(/\/+$/, '');

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
    if (this.activeRoute() !== 'cloud') {
      this.activeRoute.set('cloud');
      this.activeBaseUrl.set(this.cloudBase);
    }
  }

  public switchToLocal(): void {
    if (this.activeRoute() !== 'local') {
      this.activeRoute.set('local');
      this.activeBaseUrl.set(this.localBase);
    }
  }

  public isUsingLocal(): boolean {
    return this.activeRoute() === 'local';
  }

  public rewriteUrl(url: string, targetBase: string = this.activeBaseUrl()): string {
    if (url.startsWith(this.localBase)) {
      return url.replace(this.localBase, targetBase);
    }
    if (url.startsWith(this.cloudBase)) {
      return url.replace(this.cloudBase, targetBase);
    }
    if (url.startsWith('/api')) {
      return `${targetBase}${url.replace('/api', '')}`;
    }
    return url;
  }

  public async checkLocalAvailability(): Promise<boolean> {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 1500);

      const response = await fetch(`${this.localBase}/Config/store`, {
        method: 'GET',
        signal: controller.signal,
      });

      clearTimeout(timeoutId);
      if (response.ok) {
        this.switchToLocal();
        return true;
      }
    } catch {
    
    }

    this.switchToCloud();
    return false;
  }
}
