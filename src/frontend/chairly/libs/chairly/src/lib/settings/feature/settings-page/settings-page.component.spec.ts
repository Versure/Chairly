import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { NEVER, of, throwError } from 'rxjs';

import { SettingsApiService } from '../../data-access';
import { CompanyInfo, VatSettings } from '../../models';
import { SettingsPageComponent } from './settings-page.component';

const emptyCompanyInfo: CompanyInfo = {
  companyName: null,
  companyEmail: null,
  street: null,
  houseNumber: null,
  postalCode: null,
  city: null,
  companyPhone: null,
  ibanNumber: null,
  vatNumber: null,
  paymentPeriodDays: null,
};

const filledCompanyInfo: CompanyInfo = {
  companyName: 'Salon Mooi',
  companyEmail: 'info@salonmooi.nl',
  street: 'Kerkstraat',
  houseNumber: '1',
  postalCode: '1234 AB',
  city: 'Amsterdam',
  companyPhone: '020-1234567',
  ibanNumber: 'NL91ABNA0417164300',
  vatNumber: 'NL123456789B01',
  paymentPeriodDays: 30,
};

const defaultVatSettings: VatSettings = {
  defaultVatRate: 21,
};

describe('SettingsPageComponent', () => {
  let fixture: ComponentFixture<SettingsPageComponent>;
  let component: SettingsPageComponent;

  const mockSettingsApi = {
    getCompanyInfo: vi.fn(),
    updateCompanyInfo: vi.fn(),
    getVatSettings: vi.fn(),
    updateVatSettings: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(emptyCompanyInfo));
    mockSettingsApi.getVatSettings.mockReturnValue(of(defaultVatSettings));

    await TestBed.configureTestingModule({
      imports: [SettingsPageComponent],
      providers: [{ provide: SettingsApiService, useValue: mockSettingsApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(SettingsPageComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display main Instellingen heading', () => {
    fixture.detectChanges();
    const heading = fixture.debugElement.query(By.css('h1'));
    expect(heading.nativeElement.textContent).toContain('Instellingen');
  });

  it('should load both company info and VAT settings on init', () => {
    fixture.detectChanges();
    expect(mockSettingsApi.getCompanyInfo).toHaveBeenCalledOnce();
    expect(mockSettingsApi.getVatSettings).toHaveBeenCalledOnce();
  });

  it('should show loading indicator while loading', () => {
    mockSettingsApi.getCompanyInfo.mockReturnValue(NEVER);
    mockSettingsApi.getVatSettings.mockReturnValue(NEVER);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Instellingen laden...');
  });

  it('should populate company form with loaded data', () => {
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(filledCompanyInfo));
    fixture.detectChanges();

    const form = component['companyForm'];
    expect(form.value.companyName).toBe('Salon Mooi');
    expect(form.value.companyEmail).toBe('info@salonmooi.nl');
    expect(form.value.street).toBe('Kerkstraat');
    expect(form.value.houseNumber).toBe('1');
    expect(form.value.postalCode).toBe('1234 AB');
    expect(form.value.city).toBe('Amsterdam');
    expect(form.value.companyPhone).toBe('020-1234567');
    expect(form.value.ibanNumber).toBe('NL91ABNA0417164300');
    expect(form.value.vatNumber).toBe('NL123456789B01');
    expect(form.value.paymentPeriodDays).toBe(30);
  });

  it('should populate VAT rate control with loaded value', () => {
    mockSettingsApi.getVatSettings.mockReturnValue(of({ defaultVatRate: 9 } as VatSettings));
    fixture.detectChanges();

    expect(component['defaultVatRateControl'].value).toBe(9);
  });

  it('should call updateCompanyInfo on company form submit', () => {
    mockSettingsApi.updateCompanyInfo.mockReturnValue(of(filledCompanyInfo));
    fixture.detectChanges();

    const form = component['companyForm'];
    form.patchValue({
      companyName: 'Salon Mooi',
      companyEmail: 'info@salonmooi.nl',
    });

    component['onSubmitCompany']();

    expect(mockSettingsApi.updateCompanyInfo).toHaveBeenCalledExactlyOnceWith(
      expect.objectContaining({
        companyName: 'Salon Mooi',
        companyEmail: 'info@salonmooi.nl',
      }),
    );
  });

  it('should show company success message after save', () => {
    vi.useFakeTimers();
    mockSettingsApi.updateCompanyInfo.mockReturnValue(of(filledCompanyInfo));
    fixture.detectChanges();

    component['onSubmitCompany']();
    fixture.detectChanges();

    expect(component['saveCompanySuccess']()).toBe(true);

    vi.advanceTimersByTime(3000);
    expect(component['saveCompanySuccess']()).toBe(false);

    vi.useRealTimers();
  });

  it('should set saveCompanyError on company update failure', () => {
    mockSettingsApi.updateCompanyInfo.mockReturnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();

    component['onSubmitCompany']();
    fixture.detectChanges();

    expect(component['saveCompanyError']()).toBe('Network error');
    expect(component['isSavingCompany']()).toBe(false);
  });

  it('should call updateVatSettings on VAT save', () => {
    mockSettingsApi.updateVatSettings.mockReturnValue(of({ defaultVatRate: 9 } as VatSettings));
    fixture.detectChanges();

    component['defaultVatRateControl'].setValue(9);
    component['onSubmitVat']();

    expect(mockSettingsApi.updateVatSettings).toHaveBeenCalledWith(9);
  });

  it('should show VAT success message after save', () => {
    vi.useFakeTimers();
    mockSettingsApi.updateVatSettings.mockReturnValue(of(defaultVatSettings));
    fixture.detectChanges();

    component['onSubmitVat']();
    fixture.detectChanges();

    expect(component['saveVatSuccess']()).toBe(true);

    vi.advanceTimersByTime(3000);
    expect(component['saveVatSuccess']()).toBe(false);

    vi.useRealTimers();
  });

  it('should show VAT error message on save failure', () => {
    mockSettingsApi.updateVatSettings.mockReturnValue(throwError(() => new Error('Server error')));
    fixture.detectChanges();

    component['onSubmitVat']();
    fixture.detectChanges();

    expect(component['saveVatError']()).toBe('Server error');
    expect(component['isSavingVat']()).toBe(false);
  });

  it('should display Bedrijfsinformatie section heading', () => {
    fixture.detectChanges();
    const headings = fixture.debugElement.queryAll(By.css('h2'));
    expect(headings[0].nativeElement.textContent).toContain('Bedrijfsinformatie');
  });

  it('should display BTW-instellingen section heading', () => {
    fixture.detectChanges();
    const headings = fixture.debugElement.queryAll(By.css('h2'));
    expect(headings[1].nativeElement.textContent).toContain('BTW-instellingen');
  });

  it('should have three VAT rate options: 0%, 9%, 21%', () => {
    fixture.detectChanges();
    const options = fixture.nativeElement.querySelectorAll(
      'select option',
    ) as NodeListOf<HTMLOptionElement>;
    expect(options.length).toBe(3);
    expect(options[0].textContent?.trim()).toBe('0%');
    expect(options[1].textContent?.trim()).toBe('9%');
    expect(options[2].textContent?.trim()).toBe('21%');
  });

  it('should display the BTW description text', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Het standaard BTW-tarief wordt automatisch toegepast',
    );
  });
});
