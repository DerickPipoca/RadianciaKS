import { TenantService } from './tenant-service';
import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HttpTransportType,
  HubConnectionState,
} from '@microsoft/signalr';
import { inject, Injectable, NgZone, OnInit } from '@angular/core';
import { OrderResponseDto } from '../models/order.model';
import { BehaviorSubject, Subject } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { environment } from '../../../environment/environment';

@Injectable({
  providedIn: 'root',
})
export class SignalrService {
  private hubConnection: HubConnection | undefined;
  private zone = inject(NgZone);
  public toastrService = inject(ToastrService);
  public tenantService = inject(TenantService);

  private tenantId: string | null = null;
  private readonly hubUrl = `${environment.serverUrl}/hubs/kds`;
  public orderUpdated$ = new Subject<OrderResponseDto>();

  public orderDelivered$ = new Subject<OrderResponseDto | any>();

  private orderCanceledSource = new Subject<OrderResponseDto>();
  public orderCanceled$ = this.orderCanceledSource.asObservable();

  public connectionStatus$ = new BehaviorSubject<'Conectado' | 'Desconectado'>('Desconectado');
  public cashShiftStatus$ = new BehaviorSubject<'Aberto' | 'Fechado' | 'Carregando...'>(
    'Carregando...',
  );

  public startConnection(): void {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.tenantId = this.tenantService.getTenantId();

    if (!this.hubConnection) {
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

      this.hubConnection.on('UpdateSystemStatus', (status) => {
        if (status === 0 || status === 'Open' || status === 'Aberto' || status === 1) {
          this.cashShiftStatus$.next('Aberto');
          this.toastrService.info('Caixa aberto!');
        } else {
          this.cashShiftStatus$.next('Fechado');
          this.toastrService.warning('Caixa fechado!');
        }
      });

      this.hubConnection.onreconnected(() => {
        console.log('SignalR reconectado! Reentrando no grupo...');
        this.connectionStatus$.next('Conectado');
        this.joinKitchenGroup();
      });

      this.hubConnection.onclose(() => {
        this.connectionStatus$.next('Desconectado');
      });

      this.addListeners();
    }

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR conectado!');
        this.connectionStatus$.next('Conectado');
        this.joinKitchenGroup();
      })
      .catch((err) => {
        console.error('Erro ao conectar ao SignalR: ', err);
        this.connectionStatus$.next('Desconectado');
      });
  }

  private joinKitchenGroup(): void {
    this.tenantId = this.tenantId || this.tenantService.getTenantId();

    if (!this.tenantId) {
      console.warn('Não foi possível entrar no grupo da cozinha: TenantId não identificado.');
      return;
    }
    if (this.hubConnection) {
      this.hubConnection
        .invoke('JoinKitchenGroup', this.tenantId)
        .then(() => console.log(`Entrou no grupo da cozinha do Tenant: ${this.tenantId}`))
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
        .then(() => {
          console.log('Conexão SignalR terminada.');
          this.connectionStatus$.next('Desconectado');
        })
        .catch((err) => console.error('Erro ao parar conexão:', err));
    }
  }
}
