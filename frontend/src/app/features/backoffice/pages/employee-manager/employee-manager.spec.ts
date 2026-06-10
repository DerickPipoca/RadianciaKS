import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeManager } from './employee-manager';

describe('EmployeeManager', () => {
  let component: EmployeeManager;
  let fixture: ComponentFixture<EmployeeManager>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeManager]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeManager);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
