import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AppConfigService } from './app-config.service';
import { AppConfig } from './models/app-config.model';

describe('AppConfigService', () => {
  let service: AppConfigService;
  let httpTesting: HttpTestingController;

  const mockConfig: AppConfig = {
    keycloakUrl: 'http://localhost:8080',
    keycloakRealm: '00000000-0000-0000-0000-000000000001',
    keycloakClientId: 'chairly-frontend',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AppConfigService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('load() calls GET /api/config and stores the result', () => {
    service.load().subscribe((config) => {
      expect(config).toEqual(mockConfig);
    });

    const req = httpTesting.expectOne('/api/config');
    expect(req.request.method).toBe('GET');
    req.flush(mockConfig);

    expect(service.keycloakUrl).toBe('http://localhost:8080');
    expect(service.keycloakRealm).toBe('00000000-0000-0000-0000-000000000001');
    expect(service.keycloakClientId).toBe('chairly-frontend');
  });
});
