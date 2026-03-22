import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AdminSubscriptionStore } from '../../data-access';
import { AdminSubscriptionListItem } from '../../models';
import { SubscriptionListPageComponent } from './subscription-list-page.component';

const mockItems: AdminSubscriptionListItem[] = [
  {
    id: '1',
    salonName: 'Salon Test',
    ownerName: 'Jan Jansen',
    email: 'jan@test.nl',
    plan: 'starter',
    billingCycle: 'Monthly',
    isTrial: false,
    status: 'provisioned',
    createdAtUtc: '2026-01-01T00:00:00Z',
    provisionedAtUtc: '2026-01-02T00:00:00Z',
    cancelledAtUtc: null,
  },
];

describe('SubscriptionListPageComponent', () => {
  let fixture: ComponentFixture<SubscriptionListPageComponent>;
  let loadSubscriptionsSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    loadSubscriptionsSpy = vi.fn();

    const mockStore = {
      items: signal<AdminSubscriptionListItem[]>([]),
      totalCount: signal(0),
      isLoading: signal(false),
      page: signal(1),
      pageSize: signal(25),
      totalPages: signal(1),
      error: signal<string | null>(null),
      selectedSubscription: signal(null),
      isDetailLoading: signal(false),
      loadSubscriptions: loadSubscriptionsSpy,
    };

    await TestBed.configureTestingModule({
      imports: [SubscriptionListPageComponent],
      providers: [provideRouter([]), { provide: AdminSubscriptionStore, useValue: mockStore }],
    }).compileComponents();

    fixture = TestBed.createComponent(SubscriptionListPageComponent);
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should call loadSubscriptions on init', () => {
    fixture.detectChanges();
    expect(loadSubscriptionsSpy).toHaveBeenCalledWith({
      search: '',
      status: '',
      plan: '',
      page: 1,
      pageSize: 25,
    });
  });

  it('should render the heading', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Abonnementen');
  });

  it('should show loading state', () => {
    const store = TestBed.inject(AdminSubscriptionStore);
    (store.isLoading as ReturnType<typeof signal<boolean>>).set(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Laden...');
  });

  it('should show empty state when no items', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Geen abonnementen gevonden.');
  });

  it('should display items when available', () => {
    const store = TestBed.inject(AdminSubscriptionStore);
    (store.items as ReturnType<typeof signal<AdminSubscriptionListItem[]>>).set(mockItems);
    (store.totalCount as ReturnType<typeof signal<number>>).set(1);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Salon Test');
    expect(compiled.textContent).toContain('Jan Jansen');
  });

  it('should call loadSubscriptions with updated filters on status change', () => {
    fixture.detectChanges();
    loadSubscriptionsSpy.mockClear();

    const selects = fixture.nativeElement.querySelectorAll(
      'select',
    ) as NodeListOf<HTMLSelectElement>;
    const statusSelect = selects[0];
    statusSelect.value = 'provisioned';
    statusSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(loadSubscriptionsSpy).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'provisioned', page: 1 }),
    );
  });
});
