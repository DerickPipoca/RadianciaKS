import { TestBed } from '@angular/core/testing';

import { KdsService } from './kds-service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { OrderResponseDto } from '../models/order.model';
import { KdsStatus } from '../enums/kds-status';

describe('KdsService', () => {
  let service: KdsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [KdsService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(KdsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getPendingKdsOrders', () => {
    it('deve buscar as comandas pendentes via requisição GET em kds/pending', () => {
      const mockOrders: OrderResponseDto[] = [
        {
          id: 'order-1',
          code: 101,
          status: 'IN_PREPARATION',
          items: [],
        },
        {
          id: 'order-2',
          code: 102,
          status: 'PENDING',
          items: [],
        },
      ] as unknown as OrderResponseDto[];

      service.getPendingKdsOrders().subscribe((orders) => {
        expect(orders.length).toBe(2);
        expect(orders).toEqual(mockOrders);
      });

      const req = httpMock.expectOne('kds/pending');
      expect(req.request.method).toBe('GET');
      req.flush(mockOrders);
    });

    it('deve retornar lista vazia quando não houver pedidos na fila', () => {
      service.getPendingKdsOrders().subscribe((orders) => {
        expect(orders).toEqual([]);
      });

      const req = httpMock.expectOne('kds/pending');
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });

  describe('updateItemStatus', () => {
    it('deve enviar requisição PUT com a URL formatada e o novo status no payload', () => {
      const orderId = 'order-abc-123';
      const itemId = 'item-xyz-789';
      const newStatus = KdsStatus.Done ?? ('READY' as unknown as KdsStatus);

      service.updateItemStatus(orderId, itemId, newStatus).subscribe((response) => {
        expect(response).toBeTruthy();
      });

      const expectedUrl = `kds/${orderId}/items/${itemId}/status`;
      const req = httpMock.expectOne(expectedUrl);

      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toBe(newStatus);

      req.flush({ success: true });
    });
  });
});
