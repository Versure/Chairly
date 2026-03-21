import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HeroSectionComponent } from './hero-section.component';

describe('HeroSectionComponent', () => {
  let component: HeroSectionComponent;
  let fixture: ComponentFixture<HeroSectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeroSectionComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(HeroSectionComponent);
    fixture.componentRef.setInput('heading', 'Test heading');
    fixture.componentRef.setInput('subheading', 'Test subheading');
    fixture.componentRef.setInput('primaryCtaLabel', 'Primary');
    fixture.componentRef.setInput('primaryCtaLink', '/primary');
    fixture.componentRef.setInput('secondaryCtaLabel', 'Secondary');
    fixture.componentRef.setInput('secondaryCtaLink', '/secondary');
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render heading and subheading', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Test heading');
    expect(compiled.querySelector('p')?.textContent).toContain('Test subheading');
  });
});
