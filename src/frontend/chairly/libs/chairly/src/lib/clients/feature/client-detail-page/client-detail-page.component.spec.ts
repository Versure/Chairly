import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';
import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { of } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { ClientApiService, RecipesApiService } from '../../data-access';
import { ClientTimeline, ClientTimelineEntry } from '../../models';
import { ClientDetailPageComponent } from './client-detail-page.component';

registerLocaleData(localeNl);

const completedEntry: ClientTimelineEntry = {
  booking: {
    id: 'booking-1',
    startTime: '2026-03-15T10:00:00Z',
    endTime: '2026-03-15T10:45:00Z',
    durationMinutes: 45,
    status: 'Completed',
    staffMemberId: 'staff-1',
    staffMemberName: 'Jan de Vries',
    totalPrice: 35,
    notes: null,
    services: [
      {
        serviceId: 'svc-1',
        serviceName: 'Knippen',
        durationMinutes: 45,
        price: 35,
        sortOrder: 1,
      },
    ],
    confirmedAtUtc: '2026-03-14T08:00:00Z',
    startedAtUtc: '2026-03-15T10:00:00Z',
    completedAtUtc: '2026-03-15T10:45:00Z',
    cancelledAtUtc: null,
    noShowAtUtc: null,
  },
  recipe: {
    id: 'recipe-1',
    bookingId: 'booking-1',
    bookingDate: '2026-03-15',
    staffMemberId: 'staff-1',
    staffMemberName: 'Jan de Vries',
    title: 'Knippen + stylen',
    products: [],
    createdAtUtc: '2026-03-15T11:00:00Z',
  },
  invoice: {
    id: 'inv-1',
    invoiceNumber: 'INV-2026-001',
    invoiceDate: '2026-03-15',
    totalAmount: 35,
    status: 'Paid',
    sentAtUtc: '2026-03-15T12:00:00Z',
    paidAtUtc: '2026-03-16T09:00:00Z',
    voidedAtUtc: null,
  },
};

const scheduledEntry: ClientTimelineEntry = {
  booking: {
    id: 'booking-2',
    startTime: '2026-04-10T14:00:00Z',
    endTime: '2026-04-10T15:00:00Z',
    durationMinutes: 60,
    status: 'Scheduled',
    staffMemberId: 'staff-1',
    staffMemberName: 'Jan de Vries',
    totalPrice: 50,
    notes: null,
    services: [
      {
        serviceId: 'svc-2',
        serviceName: 'Kleuren',
        durationMinutes: 60,
        price: 50,
        sortOrder: 1,
      },
    ],
    confirmedAtUtc: null,
    startedAtUtc: null,
    completedAtUtc: null,
    cancelledAtUtc: null,
    noShowAtUtc: null,
  },
  recipe: null,
  invoice: null,
};

const mockTimeline: ClientTimeline = {
  profile: {
    id: 'client-1',
    firstName: 'Anna',
    lastName: 'Bakker',
    email: 'anna@example.com',
    phoneNumber: '0612345678',
    notes: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  stats: {
    totalVisits: 1,
    lastVisitAtUtc: '2026-03-15T10:00:00Z',
    totalSpentAmount: 35,
    mostVisitedStaffMember: { id: 'staff-1', fullName: 'Jan de Vries', visitCount: 1 },
    mostBookedService: { id: 'svc-1', name: 'Knippen', bookingCount: 1 },
    noShowCount: 0,
  },
  timeline: [scheduledEntry, completedEntry],
};

const emptyTimeline: ClientTimeline = {
  profile: {
    id: 'client-2',
    firstName: 'Piet',
    lastName: 'Jansen',
    email: null,
    phoneNumber: null,
    notes: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  },
  stats: {
    totalVisits: 0,
    lastVisitAtUtc: null,
    totalSpentAmount: 0,
    mostVisitedStaffMember: null,
    mostBookedService: null,
    noShowCount: 0,
  },
  timeline: [],
};

describe('ClientDetailPageComponent', () => {
  let fixture: ComponentFixture<ClientDetailPageComponent>;
  let mockClientApi: {
    getAll: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    getClientTimeline: ReturnType<typeof vi.fn>;
  };
  let mockRecipesApi: {
    getRecipeByBooking: ReturnType<typeof vi.fn>;
    getClientRecipes: ReturnType<typeof vi.fn>;
    createRecipe: ReturnType<typeof vi.fn>;
    updateRecipe: ReturnType<typeof vi.fn>;
  };

  function setup(timeline: ClientTimeline): void {
    mockClientApi = {
      getAll: vi.fn().mockReturnValue(of([])),
      create: vi.fn().mockReturnValue(of(timeline.profile)),
      update: vi.fn().mockReturnValue(of(timeline.profile)),
      delete: vi.fn().mockReturnValue(of(undefined)),
      getClientTimeline: vi.fn().mockReturnValue(of(timeline)),
    };

    mockRecipesApi = {
      getRecipeByBooking: vi.fn().mockReturnValue(of(null)),
      getClientRecipes: vi.fn().mockReturnValue(of([])),
      createRecipe: vi.fn().mockReturnValue(of(null)),
      updateRecipe: vi.fn().mockReturnValue(of(null)),
    };
  }

  async function createComponent(timeline: ClientTimeline): Promise<void> {
    setup(timeline);

    await TestBed.configureTestingModule({
      imports: [ClientDetailPageComponent],
      providers: [
        provideRouter([{ path: 'klanten/:clientId', component: ClientDetailPageComponent }]),
        { provide: ClientApiService, useValue: mockClientApi },
        { provide: RecipesApiService, useValue: mockRecipesApi },
        { provide: API_BASE_URL, useValue: 'https://test' },
        { provide: LOCALE_ID, useValue: 'nl-NL' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ClientDetailPageComponent);
    fixture.detectChanges();

    // Stub native dialog methods (not implemented in JSDOM)
    const dialogs = fixture.nativeElement.querySelectorAll(
      'dialog',
    ) as NodeListOf<HTMLDialogElement>;
    dialogs.forEach((d) => {
      d.showModal = vi.fn();
      d.close = vi.fn();
    });
  }

  it('renders the profile header component', async () => {
    await createComponent(mockTimeline);

    const header = fixture.nativeElement.querySelector(
      'chairly-client-profile-header',
    ) as Element | null;
    expect(header).toBeTruthy();
  });

  it('renders the timeline status filter component', async () => {
    await createComponent(mockTimeline);

    const filter = fixture.nativeElement.querySelector(
      'chairly-timeline-status-filter',
    ) as Element | null;
    expect(filter).toBeTruthy();
  });

  it('renders at least one month group with a booking timeline card', async () => {
    await createComponent(mockTimeline);

    const monthHeaders = fixture.nativeElement.querySelectorAll('h2') as NodeListOf<HTMLElement>;
    // Should have at least one month group heading (excluding the profile header h2)
    const monthGroupHeaders = Array.from(monthHeaders).filter(
      (h) => !h.closest('chairly-client-profile-header'),
    );
    expect(monthGroupHeaders.length).toBeGreaterThanOrEqual(1);

    const cards = fixture.nativeElement.querySelectorAll(
      'chairly-booking-timeline-card',
    ) as NodeListOf<Element>;
    expect(cards.length).toBeGreaterThanOrEqual(1);
  });

  it('renders the empty-state message when the timeline has zero entries', async () => {
    await createComponent(emptyTimeline);

    const paragraphs = fixture.nativeElement.querySelectorAll('p') as NodeListOf<HTMLElement>;
    const emptyMsg = Array.from(paragraphs).find(
      (p) => p.textContent?.trim() === 'Deze klant heeft nog geen boekingen.',
    );
    expect(emptyMsg).toBeTruthy();
  });

  it('does not render booking timeline cards when timeline is empty', async () => {
    await createComponent(emptyTimeline);

    const cards = fixture.nativeElement.querySelectorAll(
      'chairly-booking-timeline-card',
    ) as NodeListOf<Element>;
    expect(cards.length).toBe(0);
  });

  it('calls getClientTimeline on init', async () => {
    await createComponent(mockTimeline);

    expect(mockClientApi.getClientTimeline).toHaveBeenCalled();
  });
});
