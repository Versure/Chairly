import { ComponentFixture, TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import { SettingsApiService } from '../../data-access';
import { CompanyInfo } from '../../models';
import { CompanyInfoPageComponent } from './company-info-page.component';

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

describe('CompanyInfoPageComponent', () => {
  let fixture: ComponentFixture<CompanyInfoPageComponent>;
  let component: CompanyInfoPageComponent;

  const mockSettingsApi = {
    getCompanyInfo: vi.fn(),
    updateCompanyInfo: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(emptyCompanyInfo));

    await TestBed.configureTestingModule({
      imports: [CompanyInfoPageComponent],
      providers: [{ provide: SettingsApiService, useValue: mockSettingsApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(CompanyInfoPageComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should load company info on init', () => {
    fixture.detectChanges();
    expect(mockSettingsApi.getCompanyInfo).toHaveBeenCalledOnce();
  });

  it('should populate form with loaded data', () => {
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(filledCompanyInfo));
    fixture.detectChanges();

    const form = component['form'];
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

  it('should call update service on form submit', () => {
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(emptyCompanyInfo));
    mockSettingsApi.updateCompanyInfo.mockReturnValue(of(filledCompanyInfo));
    fixture.detectChanges();

    const form = component['form'];
    form.patchValue({
      companyName: 'Salon Mooi',
      companyEmail: 'info@salonmooi.nl',
    });

    component['onSubmit']();

    expect(mockSettingsApi.updateCompanyInfo).toHaveBeenCalledExactlyOnceWith(
      expect.objectContaining({
        companyName: 'Salon Mooi',
        companyEmail: 'info@salonmooi.nl',
      }),
    );
  });

  it('should show success message after save', () => {
    vi.useFakeTimers();
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(emptyCompanyInfo));
    mockSettingsApi.updateCompanyInfo.mockReturnValue(of(filledCompanyInfo));
    fixture.detectChanges();

    component['onSubmit']();
    fixture.detectChanges();

    expect(component['saveSuccess']()).toBe(true);

    vi.advanceTimersByTime(3000);
    expect(component['saveSuccess']()).toBe(false);

    vi.useRealTimers();
  });

  it('should set saveError on update failure', () => {
    mockSettingsApi.getCompanyInfo.mockReturnValue(of(emptyCompanyInfo));
    mockSettingsApi.updateCompanyInfo.mockReturnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();

    component['onSubmit']();
    fixture.detectChanges();

    expect(component['saveError']()).toBe('Network error');
    expect(component['isSaving']()).toBe(false);
  });
});
