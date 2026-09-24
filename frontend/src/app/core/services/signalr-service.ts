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

  public connectionStatus$ = new BehaviorSubject<'Conectado' | 'Desconectado' | 'Conectando'>(
    'Desconectado',
  );
  public cashShiftStatus$ = new BehaviorSubject<'Aberto' | 'Fechado' | 'Carregando...'>(
    'Carregando...',
  );

  public async startConnection(): Promise<void> {
    // Se já estiver conectado ou em processo de conexão, ignora chamadas duplicadas
    if (
      this.hubConnection?.state === HubConnectionState.Connected ||
      this.hubConnection?.state === HubConnectionState.Connecting
    ) {
      return;
    }

    // Se estiver a desconectar de uma chamada anterior, aguarda 300ms antes de reiniciar
    if (this.hubConnection?.state === HubConnectionState.Disconnecting) {
      setTimeout(() => this.startConnection(), 300);
      return;
    }

    this.tenantId = this.tenantService.getTenantId();

    if (!this.hubConnection) {
      this.hubConnection = new HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          skipNegotiation: true,
          transport: HttpTransportType.WebSockets,
        })
        .configureLogging(LogLevel.Warning)
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

      this.hubConnection.on('ReceiveOrderCanceled', (order: OrderResponseDto) => {
        this.orderCanceledSource.next(order);
      });

      this.hubConnection.on('UpdateSystemStatus', (status) => {
        this.zone.run(() => {
          if (status === 0 || status === 'Open' || status === 'Aberto' || status === 1) {
            this.cashShiftStatus$.next('Aberto');
            this.toastrService.info('Caixa aberto!');
          } else {
            this.cashShiftStatus$.next('Fechado');
            this.toastrService.warning('Caixa fechado!');
          }
        });
      });

      // Evento de oscilação/reconexão
      this.hubConnection.onreconnecting(() => {
        this.zone.run(() => this.connectionStatus$.next('Conectando'));
      });

      this.hubConnection.onreconnected(() => {
        this.zone.run(() => {
          console.log('[SignalR] Reconectado com sucesso!');
          this.connectionStatus$.next('Conectado');
          this.joinKitchenGroup();
        });
      });

      this.hubConnection.onclose(() => {
        this.zone.run(() => this.connectionStatus$.next('Desconectado'));
      });

      this.addListeners();
    }

    try {
      this.connectionStatus$.next('Conectando');
      await this.hubConnection.start();
      this.zone.run(() => {
        console.log('[SignalR] Conectado!');
        this.connectionStatus$.next('Conectado');
        this.joinKitchenGroup();
      });
    } catch (err) {
      console.error('[SignalR] Erro ao iniciar:', err);
      this.zone.run(() => this.connectionStatus$.next('Desconectado'));
    }
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
    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      console.trace('[SignalR] stopConnection chamado por:');
      this.hubConnection
        .stop()
        .then(() => {
          this.zone.run(() => this.connectionStatus$.next('Desconectado'));
        })
        .catch((err) => console.error('[SignalR] Erro ao parar:', err));
    }
  }
}
