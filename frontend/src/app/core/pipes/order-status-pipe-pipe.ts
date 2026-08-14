import { Pipe, PipeTransform } from '@angular/core';
import { OrderStatus } from '../enums/order-status';

@Pipe({ name: 'orderStatusLabel', standalone: true })
export class OrderStatusLabelPipe implements PipeTransform {
  transform(status: OrderStatus | string | number): string {
    const parsedStatus =
      typeof status === 'string' ? OrderStatus[status as keyof typeof OrderStatus] : status;

    switch (parsedStatus) {
      case OrderStatus.Canceled:
        return 'Cancelado';
      case OrderStatus.Preparing:
        return 'Preparando';
      case OrderStatus.ReadyToServe:
        return 'Pronto p/ servir';
      case OrderStatus.Delivered:
        return 'Entregue';
      case OrderStatus.Open:
        return 'Aberto';
      default:
        return 'Desconhecido';
    }
  }
}

@Pipe({ name: 'orderStatusClass', standalone: true })
export class OrderStatusClassPipe implements PipeTransform {
  transform(status: OrderStatus | string | number): string {
    const parsedStatus =
      typeof status === 'string' ? OrderStatus[status as keyof typeof OrderStatus] : status;

    switch (parsedStatus) {
      case OrderStatus.Canceled:
        return 'canceled';
      case OrderStatus.Preparing:
        return 'preparing';
      case OrderStatus.ReadyToServe:
        return 'ready-to-serve';
      case OrderStatus.Delivered:
        return 'delivered';
      case OrderStatus.Open:
        return 'open';
      default:
        return 'open';
    }
  }
}
