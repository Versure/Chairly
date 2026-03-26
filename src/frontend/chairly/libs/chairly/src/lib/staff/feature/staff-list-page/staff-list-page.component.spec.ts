import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { of, throwError } from 'rxjs';

import { API_BASE_URL, ConfirmationDialogComponent } from '@org/shared-lib';

import { StaffApiService, StaffStore } from '../../data-access';
import { CreateStaffMemberRequest, StaffMemberResponse } from '../../models';
import { StaffFormDialogComponent, StaffTableComponent } from '../../ui';
import { StaffListPageComponent } from './staff-list-page.component';

const mockStaff: StaffMemberResponse = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'Jansen',
  email: 'jan.jansen@salon.nl',
  role: 'staff_member',
  color: '#6366f1',
  photoUrl: null,
  isActive: true,
  schedule: {},
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

describe('StaffListPageComponent', () => {
  let fixture: ComponentFixture<StaffListPageComponent>;
  let mockApi: {
    getAll: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    deactivate: ReturnType<typeof vi.fn>;
    reactivate: ReturnType<typeof vi.fn>;
    resetPassword: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockApi = {
      getAll: vi.fn().mockReturnValue(of([])),
      create: vi.fn().mockReturnValue(of(mockStaff)),
      update: vi.fn().mockReturnValue(of(mockStaff)),
      deactivate: vi.fn().mockReturnValue(of(undefined)),
      reactivate: vi.fn().mockReturnValue(of(undefined)),
      resetPassword: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [StaffListPageComponent],
      providers: [
        StaffStore,
        { provide: StaffApiService, useValue: mockApi },
        { provide: API_BASE_URL, useValue: 'https://test' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StaffListPageComponent);
    fixture.detectChanges();

    // Stub native dialog methods (not implemented in JSDOM)
    const dialogs = fixture.nativeElement.querySelectorAll(
      'dialog',
    ) as NodeListOf<HTMLDialogElement>;
    dialogs.forEach((d) => {
      d.showModal = vi.fn();
      d.close = vi.fn();
    });
  });

  it('renders the staff table', () => {
    const table = fixture.nativeElement.querySelector('chairly-staff-table') as Element | null;
    expect(table).toBeTruthy();
  });

  it('clicking "+ Medewerker toevoegen" opens the form dialog', () => {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button[type="button"]'),
    ) as HTMLButtonElement[];
    const addButton = buttons.find(
      (b) => b.textContent?.trim() === '+ Medewerker toevoegen',
    ) as HTMLButtonElement;
    expect(addButton).toBeTruthy();
    addButton.click();
    fixture.detectChanges();

    const formDialogEl = fixture.nativeElement.querySelector(
      'chairly-staff-form-dialog dialog',
    ) as HTMLDialogElement;
    expect(formDialogEl.showModal).toHaveBeenCalled();
  });

  it('save event triggers create API call when no staff is selected', () => {
    const request: CreateStaffMemberRequest = {
      firstName: 'Anna',
      lastName: 'Bakker',
      email: 'anna.bakker@salon.nl',
      role: 'staff_member',
      color: '#6366f1',
      photoUrl: null,
      schedule: {},
    };

    const formDialogComponent = fixture.debugElement.children
      .map((de) => de.componentInstance)
      .find((c): c is StaffFormDialogComponent => c instanceof StaffFormDialogComponent);

    expect(formDialogComponent).toBeDefined();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    formDialogComponent!.saved.emit(request);
    fixture.detectChanges();

    expect(mockApi.create).toHaveBeenCalledWith(request);
  });

  it('shows error message when StaffApiService.create throws an error', () => {
    mockApi.create = vi.fn().mockReturnValue(throwError(() => new Error('Server error')));

    const request: CreateStaffMemberRequest = {
      firstName: 'Anna',
      lastName: 'Bakker',
      email: 'anna.bakker@salon.nl',
      role: 'staff_member',
      color: '#6366f1',
      photoUrl: null,
      schedule: {},
    };

    const formDialogComponent = fixture.debugElement.children
      .map((de) => de.componentInstance)
      .find((c): c is StaffFormDialogComponent => c instanceof StaffFormDialogComponent);

    expect(formDialogComponent).toBeDefined();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    formDialogComponent!.saved.emit(request);
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector(
      'p.text-red-600, p.text-red-400',
    ) as Element | null;
    expect(errorEl).toBeTruthy();
    expect(errorEl?.textContent?.trim()).toBeTruthy();
  });

  it('shows Dutch email validation message when create returns 400 with email errors', () => {
    mockApi.create = vi.fn().mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { errors: { email: ['Email is required.'] } },
          }),
      ),
    );

    const request: CreateStaffMemberRequest = {
      firstName: 'Anna',
      lastName: 'Bakker',
      email: 'anna.bakker@salon.nl',
      role: 'staff_member',
      color: '#6366f1',
      photoUrl: null,
      schedule: {},
    };

    const formDialogComponent = fixture.debugElement.children
      .map((de) => de.componentInstance)
      .find((c): c is StaffFormDialogComponent => c instanceof StaffFormDialogComponent);

    expect(formDialogComponent).toBeDefined();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    formDialogComponent!.saved.emit(request);
    fixture.detectChanges();

    const formErrorEl = fixture.nativeElement.querySelector(
      'p.text-red-600, p.text-red-400',
    ) as Element | null;
    const fieldErrorEl = fixture.nativeElement.querySelector(
      'chairly-staff-form-dialog p.text-red-600, chairly-staff-form-dialog p.text-red-400',
    ) as Element | null;
    expect(formErrorEl?.textContent?.trim()).toBe(
      'Controleer de ingevulde gegevens en probeer het opnieuw.',
    );
    expect(fieldErrorEl?.textContent?.trim()).toBe(
      'Controleer het e-mailadres. Dit veld is verplicht en moet een geldig formaat hebben.',
    );
  });

  describe('reset password flow', () => {
    function getResetPasswordDialog(): ConfirmationDialogComponent {
      const confirmDialogs = fixture.debugElement
        .queryAll(By.directive(ConfirmationDialogComponent))
        .map((de) => de.componentInstance as ConfirmationDialogComponent);
      const dialog = confirmDialogs.find((c) => c.title() === 'Wachtwoord resetten');
      expect(dialog).toBeDefined();
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
      return dialog!;
    }

    function triggerResetPassword(): void {
      const tableDe = fixture.debugElement.query(By.directive(StaffTableComponent));
      expect(tableDe).toBeTruthy();
      (tableDe.componentInstance as StaffTableComponent).resetPassword.emit(mockStaff);
      fixture.detectChanges();
    }

    it('clicking reset password on a table row opens the confirmation dialog', () => {
      // Load staff into the store so the table renders rows
      mockApi.getAll = vi.fn().mockReturnValue(of([mockStaff]));
      fixture = TestBed.createComponent(StaffListPageComponent);
      fixture.detectChanges();

      // Stub dialogs
      const dialogs = fixture.nativeElement.querySelectorAll(
        'dialog',
      ) as NodeListOf<HTMLDialogElement>;
      dialogs.forEach((d) => {
        d.showModal = vi.fn();
        d.close = vi.fn();
      });

      triggerResetPassword();

      const resetPasswordDialog = fixture.nativeElement.querySelector(
        'chairly-confirmation-dialog[title="Wachtwoord resetten"] dialog',
      ) as HTMLDialogElement;
      expect(resetPasswordDialog.showModal).toHaveBeenCalled();
    });

    it('confirming the dialog calls StaffApiService.resetPassword', () => {
      triggerResetPassword();

      getResetPasswordDialog().confirmed.emit();
      fixture.detectChanges();

      expect(mockApi.resetPassword).toHaveBeenCalledWith('staff-1');
    });

    it('success response shows success banner with staff member name', () => {
      triggerResetPassword();

      getResetPasswordDialog().confirmed.emit();
      fixture.detectChanges();

      const successBanner = fixture.nativeElement.querySelector('.bg-green-50') as Element | null;
      expect(successBanner).toBeTruthy();
      expect(successBanner?.textContent).toContain(
        'Wachtwoord-reset e-mail is verstuurd naar Jan Jansen.',
      );
    });

    it('error response shows error message', () => {
      mockApi.resetPassword = vi.fn().mockReturnValue(throwError(() => new Error('Server error')));

      triggerResetPassword();

      getResetPasswordDialog().confirmed.emit();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector(
        'p.text-red-600, p.text-red-400',
      ) as Element | null;
      expect(errorEl).toBeTruthy();
      expect(errorEl?.textContent).toContain(
        'Er is een fout opgetreden bij het versturen van de wachtwoord-reset e-mail.',
      );
    });

    it('success banner auto-clears after 5-second timeout', async () => {
      vi.useFakeTimers();

      triggerResetPassword();

      getResetPasswordDialog().confirmed.emit();
      fixture.detectChanges();

      // Banner should be visible
      let successBanner = fixture.nativeElement.querySelector('.bg-green-50') as Element | null;
      expect(successBanner).toBeTruthy();

      // Advance time by 5 seconds
      vi.advanceTimersByTime(5000);
      fixture.detectChanges();

      // Banner should be cleared
      successBanner = fixture.nativeElement.querySelector('.bg-green-50') as Element | null;
      expect(successBanner).toBeNull();

      vi.useRealTimers();
    });
  });
});
