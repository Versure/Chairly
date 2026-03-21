import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { of } from 'rxjs';

import { ConfirmationPageComponent } from './confirmation-page.component';

describe('ConfirmationPageComponent', () => {
  function createComponent(typeParam: string | null): ComponentFixture<ConfirmationPageComponent> {
    TestBed.configureTestingModule({
      imports: [ConfirmationPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: of({
              get: (key: string) => (key === 'type' ? typeParam : null),
            }),
          },
        },
      ],
    });

    const fix = TestBed.createComponent(ConfirmationPageComponent);
    fix.detectChanges();
    return fix;
  }

  it('should create', () => {
    const fixture = createComponent('abonnement');
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display subscription confirmation when type=abonnement', () => {
    const fixture = createComponent('abonnement');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Bedankt voor uw aanmelding!');
    expect(compiled.textContent).toContain('Wij verwerken uw aanvraag zo snel mogelijk');
  });

  it('should display generic confirmation for unknown type', () => {
    const fixture = createComponent('onbekend');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Bedankt voor uw aanmelding!');
  });

  it('should have a link back to home page', () => {
    const fixture = createComponent('abonnement');
    const compiled = fixture.nativeElement as HTMLElement;
    const links = compiled.querySelectorAll('a');
    const homeLink = Array.from(links).find((link) =>
      link.textContent?.includes('Terug naar home'),
    );
    expect(homeLink).toBeTruthy();
    expect(homeLink?.getAttribute('href')).toBe('/');
  });
});
