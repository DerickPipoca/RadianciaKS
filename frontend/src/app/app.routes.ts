import { Routes } from '@angular/router';
import { KdsBoard } from './features/kds/kds-board/kds-board';

export const routes: Routes = [
  { path: '', redirectTo: 'pdv', pathMatch: 'full' },
  {
    path: 'pdv',
    loadChildren: () => import('./features/pdv/pdv.routes').then((m) => m.PDV_ROUTES),
  },
  {
    path: 'admin',
    loadChildren: () =>
      import('./features/backoffice/backoffice.routes').then((m) => m.BACKOFFICE_ROUTES),
  },
  {
    path: 'kds',
    component: KdsBoard,
  },
];
