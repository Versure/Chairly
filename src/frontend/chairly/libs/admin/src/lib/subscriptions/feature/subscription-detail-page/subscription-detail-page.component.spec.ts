import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { AdminSubscriptionStore } from '../../data-access';
import { AdminSubscriptionDetail, AdminSubscriptionListItem } from '../../models';
import { SubscriptionDetailPageComponent } from './subscription-detail-page.component';

const mockDetail: AdminSubscriptionDetail = {
  id: '00000000-0000-0000-0000-000000000001',
  salonName: 'Salon Test',
  ownerFirstName: 'Jan',
  ownerLastName: 'Jansen',
  email: 'jan@test.nl',
  phoneNumber: '+31612345678',
  plan: 'starter',
  billingCycle: 'Monthly',
  isTrial: false,
  status: 'provisioned',
  trialEndsAtUtc: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  createdBy: null,
  provisionedAtUtc: '2026-01-02T00:00:00Z',
  provisionedBy: 'admin',
  cancelledAtUtc: null,
  cancelledBy: null,
  cancellationReason: null,
};

describe('SubscriptionDetailPageComponent', () => {
  let fixture: ComponentFixture<SubscriptionDetailPageComponent>;
  let loadSubscriptionSpy: ReturnType<typeof vi.fn>;
  let selectedSubscriptionSignal: ReturnType<typeof signal<AdminSubscriptionDetail | null>>;
  let isDetailLoadingSignal: ReturnType<typeof signal<boolean>>;

  beforeEach(async () => {
    // Mock dialog methods not available in JSDOM
    HTMLDialogElement.prototype.showModal = vi.fn();
    HTMLDialogElement.prototype.close = vi.fn();

    loadSubscriptionSpy = vi.fn();
    selectedSubscriptionSignal = signal<AdminSubscriptionDetail | null>(null);
    isDetailLoadingSignal = signal(false);

    const mockStore = {
      items: signal<AdminSubscriptionListItem[]>([]),
      totalCount: signal(0),
      isLoading: signal(false),
      page: signal(1),
      pageSize: signal(25),
      totalPages: signal(1),
      error: signal<string | null>(null),
      selectedSubscription: selectedSubscriptionSignal,
      isDetailLoading: isDetailLoadingSignal,
      loadSubscription: loadSubscriptionSpy,
      loadSubscriptions: vi.fn(),
      provisionSubscription: vi.fn(),
      cancelSubscription: vi.fn(),
      updateSubscriptionPlan: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [SubscriptionDetailPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              params: { id: '00000000-0000-0000-0000-000000000001' },
              queryParams: {},
            },
          },
        },
        { provide: AdminSubscriptionStore, useValue: mockStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SubscriptionDetailPageComponent);
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should call loadSubscription on init', () => {
    fixture.detectChanges();
    expect(loadSubscriptionSpy).toHaveBeenCalledWith('00000000-0000-0000-0000-000000000001');
  });

  it('should show loading state', () => {
    isDetailLoadingSignal.set(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Laden...');
  });

  it('should show not found state when no subscription', () => {
    isDetailLoadingSignal.set(false);
    selectedSubscriptionSignal.set(null);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Abonnement niet gevonden.');
  });

  it('should display subscription details when loaded', () => {
    isDetailLoadingSignal.set(false);
    selectedSubscriptionSignal.set(mockDetail);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Salon Test');
    expect(compiled.textContent).toContain('Jan Jansen');
    expect(compiled.textContent).toContain('jan@test.nl');
  });
});
