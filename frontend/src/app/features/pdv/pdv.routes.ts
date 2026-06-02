import { Routes } from '@angular/router';
import { Layout } from './layout/layout';
import { Catalog } from './pages/catalog/catalog';
import { Cart } from './pages/cart/cart';

export const PDV_ROUTES: Routes = [
  {
    path: '',
    component: Layout,
    children: [
      { path: '', redirectTo: 'catalog', pathMatch: 'full' },
      { path: 'catalog', component: Catalog },
      { path: 'cart', component: Cart },
    ],
  },
];
