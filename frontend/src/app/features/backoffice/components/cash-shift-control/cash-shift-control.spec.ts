import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CashShiftControlComponent } from './cash-shift-control';

describe('CashShiftControl', () => {
  let component: CashShiftControlComponent;
  let fixture: ComponentFixture<CashShiftControlComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CashShiftControlComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CashShiftControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
