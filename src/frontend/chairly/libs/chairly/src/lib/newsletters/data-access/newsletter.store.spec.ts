import { TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import {
  NewsletterCampaignDetail,
  NewsletterCampaignSummary,
  PreviewNewsletterResponse,
} from '../models';
import { NewsletterStore } from './newsletter.store';
import { NewslettersApiService } from './newsletters-api.service';

describe('NewsletterStore', () => {
  const summary: NewsletterCampaignSummary = {
    id: 'c1',
    subject: 'Lente-actie',
    status: 'Draft',
    recipientCount: 0,
    sentCount: 0,
    failedCount: 0,
    scheduledAtUtc: null,
    sentAtUtc: null,
    createdAtUtc: '2026-04-01T00:00:00Z',
  };

  const detail: NewsletterCampaignDetail = {
    id: 'c1',
    subject: 'Lente-actie',
    bodyHtml: '<p>Hi</p>',
    recipientFilter: 'AllSubscribed',
    status: 'Draft',
    scheduledAtUtc: null,
    queuedAtUtc: null,
    sentAtUtc: null,
    cancelledAtUtc: null,
    createdAtUtc: '2026-04-01T00:00:00Z',
    updatedAtUtc: null,
    totalRecipients: 0,
    sentCount: 0,
    failedCount: 0,
    pendingCount: 0,
    unsubscribedCount: 0,
    eligibleSubscribers: 12,
  };

  const sentDetail: NewsletterCampaignDetail = {
    ...detail,
    status: 'Sent',
    sentAtUtc: '2026-04-02T00:00:00Z',
    totalRecipients: 12,
    sentCount: 12,
  };

  const scheduledDetail: NewsletterCampaignDetail = {
    ...detail,
    status: 'Scheduled',
    scheduledAtUtc: '2026-05-01T10:00:00Z',
  };

  const cancelledDetail: NewsletterCampaignDetail = {
    ...detail,
    status: 'Cancelled',
    cancelledAtUtc: '2026-04-02T00:00:00Z',
  };

  const previewResponse: PreviewNewsletterResponse = {
    subject: 'Lente-actie',
    htmlBody: '<html><body>Hi</body></html>',
  };

  const mockApi = {
    listCampaigns: vi.fn(),
    getCampaign: vi.fn(),
    createCampaign: vi.fn(),
    updateCampaign: vi.fn(),
    deleteCampaign: vi.fn(),
    scheduleCampaign: vi.fn(),
    cancelCampaign: vi.fn(),
    sendCampaign: vi.fn(),
    testSendCampaign: vi.fn(),
    previewNewsletter: vi.fn(),
  };

  let store: InstanceType<typeof NewsletterStore>;

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [NewsletterStore, { provide: NewslettersApiService, useValue: mockApi }],
    });
    store = TestBed.inject(NewsletterStore);
  });

  it('initializes with empty state', () => {
    expect(store.campaigns()).toEqual([]);
    expect(store.isLoadingList()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('loadCampaigns populates the list', () => {
    mockApi.listCampaigns.mockReturnValue(of([summary]));
    store.loadCampaigns();
    expect(store.campaigns()).toEqual([summary]);
    expect(store.isLoadingList()).toBe(false);
  });

  it('createCampaign adds to list and sets selectedCampaign', () => {
    mockApi.createCampaign.mockReturnValue(of(detail));
    store.createCampaign({ subject: 'Lente-actie', bodyHtml: '<p>Hi</p>' });
    expect(store.selectedCampaign()).toEqual(detail);
    expect(store.campaigns().length).toBe(1);
  });

  it('sendCampaign updates selectedCampaign on success', () => {
    mockApi.sendCampaign.mockReturnValue(of(sentDetail));
    store.sendCampaign('c1');
    expect(store.selectedCampaign()).toEqual(sentDetail);
    expect(store.isSending()).toBe(false);
  });

  it('scheduleCampaign updates campaign with ScheduledAtUtc', () => {
    mockApi.scheduleCampaign.mockReturnValue(of(scheduledDetail));
    store.scheduleCampaign('c1', { scheduledAtUtc: '2026-05-01T10:00:00Z' });
    expect(store.selectedCampaign()?.status).toBe('Scheduled');
    expect(store.selectedCampaign()?.scheduledAtUtc).toBe('2026-05-01T10:00:00Z');
  });

  it('cancelCampaign flips status to Cancelled', () => {
    mockApi.cancelCampaign.mockReturnValue(of(cancelledDetail));
    store.cancelCampaign('c1');
    expect(store.selectedCampaign()?.status).toBe('Cancelled');
  });

  it('loadPreview populates preview signal', () => {
    mockApi.previewNewsletter.mockReturnValue(of(previewResponse));
    store.loadPreview({ subject: 'Lente-actie', bodyHtml: '<p>Hi</p>' });
    expect(store.preview()).toEqual(previewResponse);
  });

  it('clearPreview resets preview to null', () => {
    mockApi.previewNewsletter.mockReturnValue(of(previewResponse));
    store.loadPreview({ subject: 'Lente-actie', bodyHtml: '<p>Hi</p>' });
    store.clearPreview();
    expect(store.preview()).toBeNull();
  });

  it('captures error message on load failure', () => {
    mockApi.listCampaigns.mockReturnValue(throwError(() => new Error('Network down')));
    store.loadCampaigns();
    expect(store.error()).toBe('Network down');
    expect(store.isLoadingList()).toBe(false);
  });
});
