import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HttpTransportType,
} from '@microsoft/signalr';
import { Injectable, signal } from '@angular/core';
import { OrderItemResponseDto } from '../models/order.model';

@Injectable({
  providedIn: 'root',
})
export class SignalrService {
  private hubConnection: HubConnection | undefined;

  private readonly testTenantId = '8d1ed281-9f3b-4659-8a46-7eb26c5d550e';
  private readonly hubUrl = 'https://localhost:7047/hubs/kds';

  public newItemSignal = signal<OrderItemResponseDto | null>(null);
  public itemReadySignal = signal<OrderItemResponseDto | null>(null);

  public startConnection(): void {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

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

    this.hubConnection.on('OnNewOrder', (item: OrderItemResponseDto) => {
      console.log('Novo item recebido na cozinha!', item);
      this.newItemSignal.set(item);
    });

    this.hubConnection.on('OnItemReady', (item: OrderItemResponseDto) => {
      console.log('Item pronto na cozinha!', item);
      this.itemReadySignal.set(item);
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
