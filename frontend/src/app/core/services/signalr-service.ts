import { TenantService } from './tenant-service';
import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HttpTransportType,
  HubConnectionState,
} from '@microsoft/signalr';
import { effect, inject, Injectable, NgZone, OnInit } from '@angular/core';
import { OrderResponseDto } from '../models/order.model';
import { BehaviorSubject, Subject } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { EndpointService } from './endpoint-service';

@Injectable({
  providedIn: 'root',
})
export class SignalrService {
  private hubConnection: HubConnection | undefined;
  private currentHubUrl: string = '';

  private zone = inject(NgZone);
  public toastrService = inject(ToastrService);
  public tenantService = inject(TenantService);
  public endpointService = inject(EndpointService);

  private tenantId: string | null = null;

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

  constructor() {
    effect(() => {
      const activeRoute = this.endpointService.activeRoute();
      const targetUrl = this.getHubUrl();

      if (this.currentHubUrl && this.currentHubUrl !== targetUrl) {
        console.log(
          `[SignalR] Rota comutada para '${activeRoute}'. Reiniciando socket para: ${targetUrl}`,
        );
        this.zone.run(() => {
          this.reconnect();
        });
      }
    });
  }

  private getHubUrl(): string {
    const base = this.endpointService.activeBaseUrl();
    return `${base.replace(/\/api\/?$/, '')}/hubs/kds`;
  }

  public async startConnection(): Promise<void> {
    if (
      this.hubConnection?.state === HubConnectionState.Connected ||
      this.hubConnection?.state === HubConnectionState.Connecting
    ) {
      return;
    }

    if (this.hubConnection?.state === HubConnectionState.Disconnecting) {
      setTimeout(() => this.startConnection(), 300);
      return;
    }

    this.tenantId = this.tenantService.getTenantId();
    const targetUrl = this.getHubUrl();

    // Se o hub ainda não foi construído ou se a URL alvo mudou
    if (!this.hubConnection || this.currentHubUrl !== targetUrl) {
      if (this.hubConnection) {
        try {
          await this.hubConnection.stop();
        } catch {
          // Ignora falha de encerramento de conexão antiga
        }
      }
      this.buildConnection(targetUrl);
    }

    try {
      this.connectionStatus$.next('Conectando');
      await this.hubConnection!.start();
      this.zone.run(() => {
        console.log(`[SignalR] Conectado com sucesso em: ${targetUrl}`);
        this.connectionStatus$.next('Conectado');
        this.joinKitchenGroup();
      });
    } catch (err) {
      console.error('[SignalR] Erro ao iniciar:', err);
      this.zone.run(() => this.connectionStatus$.next('Desconectado'));
    }
  }

  public async reconnect(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
      } catch {
        // Ignora erro ao forçar desconexão
      }
      this.hubConnection = undefined;
    }
    await this.startConnection();
  }

  private buildConnection(url: string): void {
    this.currentHubUrl = url;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(url, {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .configureLogging(LogLevel.Warning)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    this.hubConnection.on('ReceiveOrderCanceled', (order: OrderResponseDto) => {
      this.zone.run(() => this.orderCanceledSource.next(order));
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
