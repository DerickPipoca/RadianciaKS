import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CloseOrders } from './close-orders';

describe('CloseOrders', () => {
  let component: CloseOrders;
  let fixture: ComponentFixture<CloseOrders>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CloseOrders]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CloseOrders);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
