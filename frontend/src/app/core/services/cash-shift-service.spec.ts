import { TestBed } from '@angular/core/testing';

import { CashShiftService } from './cash-shift-service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import {
  CashShiftHistory,
  CashShiftResponse,
  CloseCashShiftRequest,
  OpenCashShiftRequest,
} from '../models/cash-shift.model';
import { provideHttpClient } from '@angular/common/http';

describe('CashShiftService', () => {
  let service: CashShiftService;
  let httpMock: HttpTestingController;

  const mockShift: CashShiftResponse = {
    id: 'shift-123',
    openedAt: '2026-09-24T10:00:00Z',
    initialAmount: 100.0,
    currentAmount: 100.0,
    status: 'OPEN',
    operatorName: 'Operador',
  } as unknown as CashShiftResponse;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CashShiftService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CashShiftService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve inicializar com o turno atual como nulo', () => {
    expect(service.currentShiftValue).toBeNull();

    service.currentShift$.subscribe((shift) => {
      expect(shift).toBeNull();
    });
  });

  describe('getCurrentOpenShift', () => {
    it('deve buscar o turno aberto e atualizar o estado reativo', () => {
      let emittedShift: CashShiftResponse | null = null;
      service.currentShift$.subscribe((shift) => (emittedShift = shift));

      service.getCurrentOpenShift().subscribe((response) => {
        expect(response).toEqual(mockShift);
      });

      const req = httpMock.expectOne('CashShift/current');
      expect(req.request.method).toBe('GET');
      req.flush(mockShift);

      expect(emittedShift!).toEqual(mockShift);
      expect(service.currentShiftValue).toEqual(mockShift);
    });

    it('deve manter estado nulo quando não houver turno aberto no servidor', () => {
      service.getCurrentOpenShift().subscribe((response) => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne('CashShift/current');
      req.flush(null);

      expect(service.currentShiftValue).toBeNull();
    });
  });

  describe('openShift', () => {
    it('deve enviar requisição de abertura e definir o novo turno ativo', () => {
      const openRequest: OpenCashShiftRequest = {
        initialAmount: 150.0,
        notes: 'Abertura de caixa manhã',
      } as unknown as OpenCashShiftRequest;

      let emittedShift: CashShiftResponse | null = null;
      service.currentShift$.subscribe((shift) => (emittedShift = shift));

      service.openShift(openRequest).subscribe((response) => {
        expect(response).toEqual(mockShift);
      });

      const req = httpMock.expectOne('CashShift/open');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(openRequest);
      req.flush(mockShift);

      expect(emittedShift!).toEqual(mockShift);
      expect(service.currentShiftValue).toEqual(mockShift);
    });
  });

  describe('closeShift', () => {
    it('deve fechar o turno e redefinir o estado reativo para nulo', () => {
      // Simula turno já aberto anteriormente
      service['currentShiftSubject'].next(mockShift);
      expect(service.currentShiftValue).toEqual(mockShift);

      const closeRequest: CloseCashShiftRequest = {
        finalAmount: 250.0,
        notes: 'Fechamento de expediente',
      } as unknown as CloseCashShiftRequest;

      const closedResponse = {
        ...mockShift,
        status: 'CLOSED',
        closedAt: '2026-09-24T18:00:00Z',
      } as unknown as CashShiftResponse;

      let lastEmittedShift: CashShiftResponse | null = mockShift;
      service.currentShift$.subscribe((shift) => (lastEmittedShift = shift));

      service.closeShift(closeRequest).subscribe((response) => {
        expect(response).toEqual(closedResponse);
      });

      const req = httpMock.expectOne('CashShift/close');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(closeRequest);
      req.flush(closedResponse);

      // O tap do closeShift obrigatoriamente emite null
      expect(lastEmittedShift).toBeNull();
      expect(service.currentShiftValue).toBeNull();
    });
  });

  describe('getHistory', () => {
    it('deve retornar a lista histórica sem afetar o turno ativo atual', () => {
      service['currentShiftSubject'].next(mockShift);

      const mockHistory: CashShiftHistory[] = [
        { id: 'shift-1', initialAmount: 100, status: 'CLOSED' },
        { id: 'shift-2', initialAmount: 120, status: 'CLOSED' },
      ] as unknown as CashShiftHistory[];

      service.getHistory().subscribe((history) => {
        expect(history.length).toBe(2);
        expect(history).toEqual(mockHistory);
      });

      const req = httpMock.expectOne('CashShift/history');
      expect(req.request.method).toBe('GET');
      req.flush(mockHistory);

      // O turno em memória deve continuar inalterado
      expect(service.currentShiftValue).toEqual(mockShift);
    });
  });
});
