import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HttpTransportType,
} from '@microsoft/signalr';
import { inject, Injectable, NgZone } from '@angular/core';
import { OrderItemResponseDto, OrderResponseDto } from '../models/order.model';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SignalrService {
  private hubConnection: HubConnection | undefined;
  private zone = inject(NgZone);

  private readonly testTenantId = '8d1ed281-9f3b-4659-8a46-7eb26c5d550e';
  private readonly hubUrl = 'https://localhost:7047/hubs/kds';
  public orderUpdated$ = new Subject<OrderResponseDto>();

  public orderDelivered$ = new Subject<OrderResponseDto | any>();

  private orderCanceledSource = new Subject<OrderResponseDto>();
  public orderCanceled$ = this.orderCanceledSource.asObservable();

  public startConnection(): void {
    if (this.hubConnection && this.hubConnection.state !== 'Disconnected') {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveOrderCanceled', (order: OrderResponseDto) => {
      this.orderCanceledSource.next(order);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR reconectado! Reentrando no grupo...');
      this.joinKitchenGroup();
    });

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR conectado!');
        this.joinKitchenGroup();
        this.addListeners();
      })
      .catch((err) => console.error('Erro ao conectar ao SignalR: ', err));
  }

  private joinKitchenGroup(): void {
    if (this.hubConnection) {
      this.hubConnection
        .invoke('JoinKitchenGroup', this.testTenantId)
        .then(() => console.log(`Entrou no grupo da cozinha do Tenant: ${this.testTenantId}`))
        .catch((err) => console.error('Erro ao entrar no grupo da cozinha:', err));
    }
  }

  private addListeners(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('OnOrderUpdated', (order: OrderResponseDto) => {
      this.zone.run(() => {
        console.log('Comanda atualizada recebida!', order);
        this.orderUpdated$.next(order);
      });
    });

    this.hubConnection.on('OnItemDelivered', (order: any) => {
      this.zone.run(() => {
        console.log('Pedido entregue ao cliente!', order);
        this.orderDelivered$.next(order);
      });
    });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection
        .stop()
        .then(() => console.log('Conexão SignalR terminada.'))
        .catch((err) => console.error('Erro ao parar conexão:', err));
    }
  }
}
