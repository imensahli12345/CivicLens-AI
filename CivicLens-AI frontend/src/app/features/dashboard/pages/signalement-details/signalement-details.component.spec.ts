import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SignalementDetailsComponent } from './signalement-details.component';

describe('SignalementDetailsComponent', () => {
  let component: SignalementDetailsComponent;
  let fixture: ComponentFixture<SignalementDetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignalementDetailsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SignalementDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
