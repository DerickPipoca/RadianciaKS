import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PromotionManager } from './promotion-manager';

describe('PromotionManager', () => {
  let component: PromotionManager;
  let fixture: ComponentFixture<PromotionManager>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PromotionManager]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PromotionManager);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
