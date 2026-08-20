import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CashShiftControl } from './cash-shift-control';

describe('CashShiftControl', () => {
  let component: CashShiftControl;
  let fixture: ComponentFixture<CashShiftControl>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CashShiftControl]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CashShiftControl);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
