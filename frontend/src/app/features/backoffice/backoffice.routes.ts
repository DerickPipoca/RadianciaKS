import { Routes } from '@angular/router';
import { Layout } from './layout/layout';
import { CategoryManager } from './pages/category-manager/category-manager';
import { ProductManager } from './pages/product-manager/product-manager';
import { EmployeeManager } from './pages/employee-manager/employee-manager';
import { Dashboard } from './pages/dashboard/dashboard';
import { StoreSettings } from './pages/store-settings/store-settings';
import { OrderManager } from './pages/order-manager/order-manager';
import { PromotionManager } from './pages/promotion-manager/promotion-manager';

export const BACKOFFICE_ROUTES: Routes = [
  {
    path: '',
    component: Layout,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: Dashboard },
      { path: 'categorias', component: CategoryManager },
      { path: 'pedidos', component: OrderManager },
      { path: 'produtos', component: ProductManager },
      { path: 'promocoes', component: PromotionManager },
      { path: 'equipe', component: EmployeeManager },
      { path: 'configuracoes-loja', component: StoreSettings },
    ],
  },
];
