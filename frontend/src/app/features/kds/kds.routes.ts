import { Routes } from '@angular/router';
import { Layout } from './layout/layout';
import { KdsBoard } from './pages/kds-board/kds-board';
import { KdsFilters } from './pages/kds-filters/kds-filters';
import { KdsHistory } from './pages/kds-history/kds-history';
export const KDS_ROUTES: Routes = [
  {
    path: '',
    component: Layout,
    children: [
      { path: '', redirectTo: 'tabela', pathMatch: 'full' },
      { path: 'tabela', component: KdsBoard },
      { path: 'filtros', component: KdsFilters },
      { path: 'historico', component: KdsHistory },
    ],
  },
];
