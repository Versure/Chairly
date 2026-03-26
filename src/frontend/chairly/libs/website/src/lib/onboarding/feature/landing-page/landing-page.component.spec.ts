import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { LandingPageComponent } from './landing-page.component';

describe('LandingPageComponent', () => {
  let component: LandingPageComponent;
  let fixture: ComponentFixture<LandingPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LandingPageComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(LandingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render hero section with heading', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('De salon software die voor u werkt');
  });

  it('should render feature cards', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('chairly-web-feature-card');
    expect(cards.length).toBe(4);
  });

  it('should have navigation links to pricing and subscribe pages', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const links = compiled.querySelectorAll('a');
    const hrefs = Array.from(links).map((link) => link.getAttribute('href'));
    expect(hrefs).toContain('/prijzen');
    expect(hrefs.some((href) => href?.startsWith('/abonneren'))).toBe(true);
  });

  it('should render the "Waarom Chairly?" section', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Waarom Chairly?');
  });

  it('should render pricing summary section', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Eenvoudige, transparante prijzen');
    expect(compiled.textContent).toContain('Starter');
    expect(compiled.textContent).toContain('Team');
    expect(compiled.textContent).toContain('Salon');
  });

  it('should render social proof section with statistics', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Vertrouwd door salons in heel Nederland');
    expect(compiled.textContent).toContain('500+');
    expect(compiled.textContent).toContain('50.000+');
    expect(compiled.textContent).toContain('98%');
  });

  it('should render CTA section', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Klaar om te starten?');
  });

  it('should not contain header or footer (provided by layout)', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('chairly-web-header')).toBeFalsy();
    expect(compiled.querySelector('chairly-web-footer')).toBeFalsy();
  });
});
