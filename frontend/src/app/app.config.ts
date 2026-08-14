import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { tenantInterceptor } from './core/interceptors/tenant-interceptor';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { authInterceptor } from './core/interceptors/auth-interceptor';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { loadingInterceptor } from './core/interceptors/loading-interceptor';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideToastr } from 'ngx-toastr';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimationsAsync(),
    provideToastr({
      timeOut: 3000,
      positionClass: 'toast-top-left',
      preventDuplicates: true,
    }),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([
        tenantInterceptor,
        loadingInterceptor,
        loadingInterceptor,
        errorInterceptor,
        authInterceptor,
      ]),
    ),
    provideCharts(withDefaultRegisterables()),
  ],
};
