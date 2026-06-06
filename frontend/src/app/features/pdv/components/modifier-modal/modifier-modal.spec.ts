import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModifierModal } from './modifier-modal';

describe('ModifierModal', () => {
  let component: ModifierModal;
  let fixture: ComponentFixture<ModifierModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModifierModal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ModifierModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
