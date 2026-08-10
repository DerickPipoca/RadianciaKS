import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KdsHistory } from './kds-history';

describe('KdsHistory', () => {
  let component: KdsHistory;
  let fixture: ComponentFixture<KdsHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KdsHistory]
    })
    .compileComponents();

    fixture = TestBed.createComponent(KdsHistory);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
