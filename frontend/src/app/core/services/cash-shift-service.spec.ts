import { TestBed } from '@angular/core/testing';

import { CashShiftService } from './cash-shift-service';

describe('CashShiftService', () => {
  let service: CashShiftService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CashShiftService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
