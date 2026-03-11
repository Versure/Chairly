import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import { VatSettings } from '../models';
import { SettingsApiService } from './settings-api.service';

describe('SettingsApiService', () => {
  let service: SettingsApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(SettingsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getVatSettings()', () => {
    it('should GET /api/settings/vat and return VatSettings', () => {
      const mockSettings: VatSettings = { defaultVatRate: 21 };

      service.getVatSettings().subscribe((result) => {
        expect(result).toEqual(mockSettings);
      });

      const req = httpTesting.expectOne('/api/settings/vat');
      expect(req.request.method).toBe('GET');
      req.flush(mockSettings);
    });
  });

  describe('updateVatSettings()', () => {
    it('should PUT /api/settings/vat with defaultVatRate in body', () => {
      const updatedSettings: VatSettings = { defaultVatRate: 9 };

      service.updateVatSettings(9).subscribe((result) => {
        expect(result).toEqual(updatedSettings);
      });

      const req = httpTesting.expectOne('/api/settings/vat');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ defaultVatRate: 9 });
      req.flush(updatedSettings);
    });
  });
});
