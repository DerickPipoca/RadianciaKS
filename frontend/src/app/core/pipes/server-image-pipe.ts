import { inject, Pipe, PipeTransform } from '@angular/core';
import { EndpointService } from '../services/endpoint-service';

@Pipe({
  name: 'serverImage',
  standalone: true,
  pure: false,
})
export class ServerImagePipe implements PipeTransform {
  private endpointService = inject(EndpointService);

  private readonly defaultFallback = 'assets/images/placeholder.png';

  transform(imagePath: string | null | undefined, customFallback?: string): string {
    const fallback = customFallback || this.defaultFallback;

    console.log('[ServerImagePipe] Entrada:', imagePath);

    if (!imagePath || !imagePath.trim()) {
      console.log('[ServerImagePipe] Vazio, usando fallback:', fallback);
      return fallback;
    }

    const path = imagePath.trim().replace(/\\/g, '/');

    if (
      path.startsWith('http://') ||
      path.startsWith('https://') ||
      path.startsWith('data:image/')
    ) {
      return path;
    }

    const rawApiBase = this.endpointService.activeBaseUrl();
    const serverRoot = rawApiBase.replace(/\/api\/?$/, '');
    const cleanPath = path.startsWith('/') ? path : `/${path}`;

    const finalUrl = `${serverRoot}${cleanPath}`;
    console.log('[ServerImagePipe] URL Final Gerada:', finalUrl);

    return `${serverRoot}${cleanPath}`;
  }
}
