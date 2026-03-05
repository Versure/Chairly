import { ComponentFixture, TestBed } from '@angular/core/testing';

import { of } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { StaffApiService, StaffStore } from '../../data-access';
import { CreateStaffMemberRequest, StaffMemberResponse } from '../../models';
import { StaffFormDialogComponent } from '../../ui';
import { StaffListPageComponent } from './staff-list-page.component';

const mockStaff: StaffMemberResponse = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'Jansen',
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
  };

  beforeEach(async () => {
    mockApi = {
      getAll: vi.fn().mockReturnValue(of([])),
      create: vi.fn().mockReturnValue(of(mockStaff)),
      update: vi.fn().mockReturnValue(of(mockStaff)),
      deactivate: vi.fn().mockReturnValue(of(undefined)),
      reactivate: vi.fn().mockReturnValue(of(undefined)),
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
      role: 'staff_member',
      color: '#6366f1',
      photoUrl: null,
      schedule: {},
    };

    const formDialogComponent =
      fixture.debugElement.children
        .map((de) => de.componentInstance)
        .find((c): c is StaffFormDialogComponent => c instanceof StaffFormDialogComponent);

    expect(formDialogComponent).toBeDefined();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    formDialogComponent!.saved.emit(request);
    fixture.detectChanges();

    expect(mockApi.create).toHaveBeenCalledWith(request);
  });
});
