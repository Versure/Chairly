import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;
  let documentEl: Document;

  function createService(storedTheme?: string): ThemeService {
    const localStorageMock: Record<string, string> = {};
    if (storedTheme !== undefined) {
      localStorageMock['chairly-theme'] = storedTheme;
    }

    const windowMock = {
      localStorage: {
        getItem: (key: string): string | null => localStorageMock[key] ?? null,
        setItem: (key: string, value: string): void => {
          localStorageMock[key] = value;
        },
      },
    };

    const documentMock = {
      documentElement: document.createElement('html'),
      defaultView: windowMock,
    };

    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: DOCUMENT, useValue: documentMock },
      ],
    });

    service = TestBed.inject(ThemeService);
    documentEl = TestBed.inject(DOCUMENT);
    return service;
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should create', () => {
    createService();
    expect(service).toBeTruthy();
  });

  it('should default to light theme when no preference is stored', () => {
    createService();
    expect(service.isDark()).toBe(false);
  });

  it('should restore dark theme from localStorage', () => {
    createService('dark');
    expect(service.isDark()).toBe(true);
  });

  it('should set data-theme="dark" on html element when dark theme is restored', () => {
    createService('dark');
    expect(documentEl.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('should not set data-theme attribute when light theme is restored', () => {
    createService('light');
    expect(service.isDark()).toBe(false);
    expect(documentEl.documentElement.getAttribute('data-theme')).toBeNull();
  });

  it('should toggle from light to dark', () => {
    createService();
    service.toggle();
    expect(service.isDark()).toBe(true);
    expect(documentEl.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('should toggle from dark to light', () => {
    createService('dark');
    service.toggle();
    expect(service.isDark()).toBe(false);
    expect(documentEl.documentElement.getAttribute('data-theme')).toBeNull();
  });

  it('should persist dark theme to localStorage on toggle', () => {
    const localStorageMock: Record<string, string> = {};
    const windowMock = {
      localStorage: {
        getItem: (key: string): string | null => localStorageMock[key] ?? null,
        setItem: (key: string, value: string): void => {
          localStorageMock[key] = value;
        },
      },
    };
    const documentMock = {
      documentElement: document.createElement('html'),
      defaultView: windowMock,
    };

    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: DOCUMENT, useValue: documentMock },
      ],
    });
    service = TestBed.inject(ThemeService);

    service.toggle();
    expect(localStorageMock['chairly-theme']).toBe('dark');

    service.toggle();
    expect(localStorageMock['chairly-theme']).toBe('light');
  });
});
