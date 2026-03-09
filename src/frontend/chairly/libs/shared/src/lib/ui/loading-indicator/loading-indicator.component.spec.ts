import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadingIndicatorComponent } from './loading-indicator.component';

describe('LoadingIndicatorComponent', () => {
  let component: LoadingIndicatorComponent;
  let fixture: ComponentFixture<LoadingIndicatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingIndicatorComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(LoadingIndicatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display default message "Laden..."', () => {
    const span = fixture.nativeElement.querySelector('span') as HTMLSpanElement;
    expect(span.textContent).toBe('Laden...');
  });

  it('should display a custom message when provided', () => {
    fixture.componentRef.setInput('message', 'Diensten laden...');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span') as HTMLSpanElement;
    expect(span.textContent).toBe('Diensten laden...');
  });

  it('should render spinner SVG', () => {
    const svg = fixture.nativeElement.querySelector('svg.animate-spin') as SVGElement | null;
    expect(svg).toBeTruthy();
  });

  it('should apply correct dark mode classes', () => {
    const container = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(container.classList.contains('dark:text-slate-400')).toBe(true);
  });
});
