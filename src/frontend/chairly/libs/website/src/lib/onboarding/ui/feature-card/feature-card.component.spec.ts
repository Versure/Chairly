import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FeatureCardComponent } from './feature-card.component';

describe('FeatureCardComponent', () => {
  let component: FeatureCardComponent;
  let fixture: ComponentFixture<FeatureCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeatureCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FeatureCardComponent);
    fixture.componentRef.setInput('title', 'Test titel');
    fixture.componentRef.setInput('description', 'Test beschrijving');
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render title and description', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h3')?.textContent).toContain('Test titel');
    expect(compiled.querySelector('p')?.textContent).toContain('Test beschrijving');
  });
});
