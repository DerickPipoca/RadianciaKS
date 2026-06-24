import { Component } from '@angular/core';
import { Catalog } from "../catalog/catalog";
import { Cart } from "../cart/cart";

@Component({
  selector: 'app-order',
  imports: [Catalog, Cart],
  templateUrl: './order.html',
  styleUrl: './order.scss',
})
export class Order {

}
