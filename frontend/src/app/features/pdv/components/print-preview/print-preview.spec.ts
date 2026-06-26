import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PrintPreview } from './print-preview';

describe('PrintPreview', () => {
  let component: PrintPreview;
  let fixture: ComponentFixture<PrintPreview>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PrintPreview]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PrintPreview);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
