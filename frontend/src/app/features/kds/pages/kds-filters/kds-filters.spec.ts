import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KdsFilters } from './kds-filters';

describe('KdsFilters', () => {
  let component: KdsFilters;
  let fixture: ComponentFixture<KdsFilters>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KdsFilters]
    })
    .compileComponents();

    fixture = TestBed.createComponent(KdsFilters);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
