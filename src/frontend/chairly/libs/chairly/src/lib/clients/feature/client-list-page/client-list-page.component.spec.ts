import { ComponentFixture, TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { ClientApiService, ClientStore } from '../../data-access';
import { ClientResponse,CreateClientRequest } from '../../models';
import { ClientFormDialogComponent } from '../../ui';
import { ClientListPageComponent } from './client-list-page.component';

const mockClient: ClientResponse = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: null,
  notes: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

describe('ClientListPageComponent', () => {
  let fixture: ComponentFixture<ClientListPageComponent>;
  let mockApi: {
    getAll: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockApi = {
      getAll: vi.fn().mockReturnValue(of([])),
      create: vi.fn().mockReturnValue(of(mockClient)),
      update: vi.fn().mockReturnValue(of(mockClient)),
      delete: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [ClientListPageComponent],
      providers: [
        ClientStore,
        { provide: ClientApiService, useValue: mockApi },
        { provide: API_BASE_URL, useValue: 'https://test' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ClientListPageComponent);
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

  it('renders the client table', () => {
    const table = fixture.nativeElement.querySelector('chairly-client-table') as Element | null;
    expect(table).toBeTruthy();
  });

  it('clicking "+ Klant toevoegen" calls formDialogRef().open(null)', () => {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button[type="button"]'),
    ) as HTMLButtonElement[];
    const addButton = buttons.find(
      (b) => b.textContent?.trim() === '+ Klant toevoegen',
    ) as HTMLButtonElement;
    expect(addButton).toBeTruthy();
    addButton.click();
    fixture.detectChanges();

    const formDialogEl = fixture.nativeElement.querySelector(
      'chairly-client-form-dialog dialog',
    ) as HTMLDialogElement;
    expect(formDialogEl.showModal).toHaveBeenCalled();
  });

  it('save event triggers create API call when no client is selected', () => {
    const request: CreateClientRequest = {
      firstName: 'Anna',
      lastName: 'Bakker',
      email: null,
      phoneNumber: null,
      notes: null,
    };

    const formDialogComponent =
      fixture.debugElement.children
        .map((de) => de.componentInstance)
        .find((c): c is ClientFormDialogComponent => c instanceof ClientFormDialogComponent);

    expect(formDialogComponent).toBeDefined();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    formDialogComponent!.saved.emit(request);
    fixture.detectChanges();

    expect(mockApi.create).toHaveBeenCalledWith(request);
  });

  it('shows error message when ClientApiService.create throws an error', () => {
    mockApi.create = vi.fn().mockReturnValue(throwError(() => new Error('Server error')));

    const request: CreateClientRequest = {
      firstName: 'Anna',
      lastName: 'Bakker',
      email: null,
      phoneNumber: null,
      notes: null,
    };

    const formDialogComponent =
      fixture.debugElement.children
        .map((de) => de.componentInstance)
        .find((c): c is ClientFormDialogComponent => c instanceof ClientFormDialogComponent);

    expect(formDialogComponent).toBeDefined();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    formDialogComponent!.saved.emit(request);
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('p.text-red-600, p.text-red-400') as Element | null;
    expect(errorEl).toBeTruthy();
    expect(errorEl?.textContent?.trim()).toBeTruthy();
  });
});
