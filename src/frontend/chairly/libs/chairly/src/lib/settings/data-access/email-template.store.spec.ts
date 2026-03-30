import { TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import { EmailTemplateResponse, PreviewEmailTemplateResponse } from '../models';
import { EmailTemplateStore } from './email-template.store';
import { EmailTemplateApiService } from './email-template-api.service';

const mockTemplates: EmailTemplateResponse[] = [
  {
    templateType: 'BookingConfirmation',
    subject: 'Bevestiging',
    mainMessage: 'Uw afspraak is bevestigd.',
    closingMessage: 'Tot ziens!',
    dateLabel: null,
    servicesLabel: null,
    isCustomized: false,
    availablePlaceholders: ['clientName', 'salonName', 'date', 'services'],
  },
  {
    templateType: 'BookingReminder',
    subject: 'Herinnering',
    mainMessage: 'U heeft morgen een afspraak.',
    closingMessage: 'Tot morgen!',
    dateLabel: null,
    servicesLabel: null,
    isCustomized: false,
    availablePlaceholders: ['clientName', 'salonName', 'date', 'services'],
  },
];

const mockPreview: PreviewEmailTemplateResponse = {
  subject: 'Test onderwerp',
  htmlBody: '<html><body>test</body></html>',
};

describe('EmailTemplateStore', () => {
  let mockApi: {
    getEmailTemplates: ReturnType<typeof vi.fn>;
    updateEmailTemplate: ReturnType<typeof vi.fn>;
    resetEmailTemplate: ReturnType<typeof vi.fn>;
    previewEmailTemplate: ReturnType<typeof vi.fn>;
  };

  function createStore(): InstanceType<typeof EmailTemplateStore> {
    TestBed.configureTestingModule({
      providers: [EmailTemplateStore, { provide: EmailTemplateApiService, useValue: mockApi }],
    });
    return TestBed.inject(EmailTemplateStore);
  }

  beforeEach(() => {
    mockApi = {
      getEmailTemplates: vi.fn(),
      updateEmailTemplate: vi.fn(),
      resetEmailTemplate: vi.fn(),
      previewEmailTemplate: vi.fn(),
    };
  });

  it('loadTemplates sets isLoading to true while loading and false after completion', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    const store = createStore();

    store.loadTemplates();

    expect(store.isLoading()).toBe(false);
    expect(store.templates()).toEqual(mockTemplates);
  });

  it('loadTemplates populates templates with the API response', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    const store = createStore();

    store.loadTemplates();

    expect(store.templates()).toEqual(mockTemplates);
  });

  it('loadTemplates sets isLoading to false and keeps templates empty on error', () => {
    mockApi.getEmailTemplates.mockReturnValue(throwError(() => new Error('fail')));
    const store = createStore();

    store.loadTemplates();

    expect(store.isLoading()).toBe(false);
    expect(store.templates()).toEqual([]);
    expect(store.error()).toBe('fail');
  });

  it('updateTemplate sets isSaving to true while saving and false after completion', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    const updated: EmailTemplateResponse = {
      ...mockTemplates[0],
      subject: 'Nieuw onderwerp',
      isCustomized: true,
    };
    mockApi.updateEmailTemplate.mockReturnValue(of(updated));
    const store = createStore();

    store.loadTemplates();
    store.updateTemplate('BookingConfirmation', {
      subject: 'Nieuw onderwerp',
      mainMessage: mockTemplates[0].mainMessage,
      closingMessage: mockTemplates[0].closingMessage,
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.isSaving()).toBe(false);
  });

  it('updateTemplate updates the matching template in the templates array', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    const updated: EmailTemplateResponse = {
      ...mockTemplates[0],
      subject: 'Nieuw onderwerp',
      isCustomized: true,
    };
    mockApi.updateEmailTemplate.mockReturnValue(of(updated));
    const store = createStore();

    store.loadTemplates();
    store.updateTemplate('BookingConfirmation', {
      subject: 'Nieuw onderwerp',
      mainMessage: mockTemplates[0].mainMessage,
      closingMessage: mockTemplates[0].closingMessage,
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.templates()[0].subject).toBe('Nieuw onderwerp');
    expect(store.templates()[0].isCustomized).toBe(true);
  });

  it('updateTemplate sets saveSuccess to true on success', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    mockApi.updateEmailTemplate.mockReturnValue(of(mockTemplates[0]));
    const store = createStore();

    store.loadTemplates();
    store.updateTemplate('BookingConfirmation', {
      subject: 'test',
      mainMessage: 'test',
      closingMessage: 'test',
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.saveSuccess()).toBe(true);
  });

  it('updateTemplate sets saveError with error message on failure', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    mockApi.updateEmailTemplate.mockReturnValue(throwError(() => new Error('save failed')));
    const store = createStore();

    store.loadTemplates();
    store.updateTemplate('BookingConfirmation', {
      subject: 'test',
      mainMessage: 'test',
      closingMessage: 'test',
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.saveError()).toBe('save failed');
  });

  it('resetTemplate reloads all templates after successful reset', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    mockApi.resetEmailTemplate.mockReturnValue(of(undefined));
    const store = createStore();

    store.loadTemplates();
    store.resetTemplate('BookingConfirmation');

    // getEmailTemplates called twice: once for loadTemplates, once after reset
    expect(mockApi.getEmailTemplates).toHaveBeenCalledTimes(2);
  });

  it('previewTemplate sets isLoadingPreview to true while loading', () => {
    mockApi.previewEmailTemplate.mockReturnValue(of(mockPreview));
    const store = createStore();

    store.previewTemplate({
      templateType: 'BookingConfirmation',
      subject: 'test',
      mainMessage: 'test',
      closingMessage: 'test',
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.isLoadingPreview()).toBe(false);
  });

  it('previewTemplate populates preview with the API response', () => {
    mockApi.previewEmailTemplate.mockReturnValue(of(mockPreview));
    const store = createStore();

    store.previewTemplate({
      templateType: 'BookingConfirmation',
      subject: 'test',
      mainMessage: 'test',
      closingMessage: 'test',
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.preview()).toEqual(mockPreview);
  });

  it('previewTemplate sets isLoadingPreview to false on error', () => {
    mockApi.previewEmailTemplate.mockReturnValue(throwError(() => new Error('preview failed')));
    const store = createStore();

    store.previewTemplate({
      templateType: 'BookingConfirmation',
      subject: 'test',
      mainMessage: 'test',
      closingMessage: 'test',
      dateLabel: null,
      servicesLabel: null,
    });

    expect(store.isLoadingPreview()).toBe(false);
    expect(store.preview()).toBeNull();
  });

  it('templatesByType computed signal returns templates indexed by type', () => {
    mockApi.getEmailTemplates.mockReturnValue(of(mockTemplates));
    const store = createStore();

    store.loadTemplates();

    const byType = store.templatesByType();
    expect(byType['BookingConfirmation']).toEqual(mockTemplates[0]);
    expect(byType['BookingReminder']).toEqual(mockTemplates[1]);
  });
});
