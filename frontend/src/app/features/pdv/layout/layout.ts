import { Component } from '@angular/core';
import { Catalog } from "../pages/catalog/catalog";
import { Cart } from "../pages/cart/cart";

@Component({
  selector: 'app-layout',
  imports: [Catalog, Cart],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout {

}
