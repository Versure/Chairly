import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { NEVER, of, throwError } from 'rxjs';

import { SettingsApiService } from '../../data-access';
import { VatSettings } from '../../models';
import { VatSettingsPageComponent } from './vat-settings-page.component';

describe('VatSettingsPageComponent', () => {
  let fixture: ComponentFixture<VatSettingsPageComponent>;
  let component: VatSettingsPageComponent;

  const mockSettingsApi = {
    getVatSettings: vi.fn(),
    updateVatSettings: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockSettingsApi.getVatSettings.mockReturnValue(of({ defaultVatRate: 21 } as VatSettings));

    await TestBed.configureTestingModule({
      imports: [VatSettingsPageComponent],
      providers: [{ provide: SettingsApiService, useValue: mockSettingsApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(VatSettingsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load VAT settings on init', () => {
    expect(mockSettingsApi.getVatSettings).toHaveBeenCalledOnce();
  });

  it('should display the BTW-instellingen heading', () => {
    const heading = fixture.debugElement.query(By.css('h1'));
    expect(heading.nativeElement.textContent).toContain('BTW-instellingen');
  });

  it('should display the description text', () => {
    expect(fixture.nativeElement.textContent).toContain(
      'Het standaard BTW-tarief wordt automatisch toegepast',
    );
  });

  it('should show loading text while loading', () => {
    mockSettingsApi.getVatSettings.mockReturnValue(NEVER);

    const newFixture = TestBed.createComponent(VatSettingsPageComponent);
    newFixture.detectChanges();

    expect(newFixture.nativeElement.textContent).toContain('Laden...');
  });

  it('should populate form control with current default VAT rate', () => {
    expect(component['defaultVatRateControl'].value).toBe(21);
  });

  it('should call updateVatSettings on save', () => {
    mockSettingsApi.updateVatSettings.mockReturnValue(of({ defaultVatRate: 9 } as VatSettings));

    const button = fixture.debugElement.query(By.css('button')).nativeElement as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(mockSettingsApi.updateVatSettings).toHaveBeenCalledWith(21);
  });

  it('should show success message after save', () => {
    mockSettingsApi.updateVatSettings.mockReturnValue(of({ defaultVatRate: 21 } as VatSettings));

    const button = fixture.debugElement.query(By.css('button')).nativeElement as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Instellingen opgeslagen');
  });

  it('should show error message on save failure', () => {
    mockSettingsApi.updateVatSettings.mockReturnValue(throwError(() => new Error('Server error')));

    const button = fixture.debugElement.query(By.css('button')).nativeElement as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Server error');
  });

  it('should have three VAT rate options: 0%, 9%, 21%', () => {
    const options = fixture.nativeElement.querySelectorAll(
      'select option',
    ) as NodeListOf<HTMLOptionElement>;
    expect(options.length).toBe(3);
    expect(options[0].textContent?.trim()).toBe('0%');
    expect(options[1].textContent?.trim()).toBe('9%');
    expect(options[2].textContent?.trim()).toBe('21%');
  });
});
