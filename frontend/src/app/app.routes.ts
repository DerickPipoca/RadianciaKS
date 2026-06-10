import { Routes } from '@angular/router';
import { KdsBoard } from './features/kds/kds-board/kds-board';
import { Login } from './features/auth/login/login';
import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    component: Login,
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'pdv',
    loadChildren: () => import('./features/pdv/pdv.routes').then((m) => m.PDV_ROUTES),
    canActivate: [authGuard],
  },
  {
    path: 'admin',
    loadChildren: () =>
      import('./features/backoffice/backoffice.routes').then((m) => m.BACKOFFICE_ROUTES),
    canActivate: [authGuard],
  },
  {
    path: 'kds',
    component: KdsBoard,
  },
];
