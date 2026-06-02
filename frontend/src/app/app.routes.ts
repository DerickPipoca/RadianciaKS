import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'pdv', pathMatch: 'full' },
  {
    path: 'pdv',
    loadChildren: () => import('./features/pdv/pdv.routes').then((m) => m.PDV_ROUTES),
  },
];
