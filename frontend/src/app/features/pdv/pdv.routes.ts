import { Routes } from '@angular/router';
import { Layout } from './layout/layout';
import { Home } from './pages/home/home';
import { Order } from './pages/order/order';
import { Orders } from './pages/orders/orders';
import { Checkout } from './pages/checkout/checkout';
import { CloseOrders } from './pages/close-orders/close-orders';

export const PDV_ROUTES: Routes = [
  {
    path: '',
    component: Layout,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: Home },
      { path: 'novo-pedido', component: Order },
      { path: 'fechar-pedidos', component: CloseOrders },
      { path: 'pedidos', component: Orders },
      { path: 'checkout', component: Checkout },
    ],
  },
];
