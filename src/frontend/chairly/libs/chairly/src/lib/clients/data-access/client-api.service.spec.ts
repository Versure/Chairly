import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import { ClientResponse, CreateClientRequest, UpdateClientRequest } from '../models';
import { ClientApiService } from './client-api.service';

describe('ClientApiService', () => {
  let service: ClientApiService;
  let httpTesting: HttpTestingController;

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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(ClientApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll()', () => {
    it('should GET /api/clients and return clients', () => {
      const mockList: ClientResponse[] = [mockClient];

      service.getAll().subscribe((clients) => {
        expect(clients).toEqual(mockList);
      });

      const req = httpTesting.expectOne('/api/clients');
      expect(req.request.method).toBe('GET');
      req.flush(mockList);
    });
  });

  describe('create()', () => {
    it('should POST /api/clients with request body and return created client', () => {
      const request: CreateClientRequest = {
        firstName: 'Anna',
        lastName: 'Bakker',
        email: 'anna@example.com',
        phoneNumber: null,
        notes: null,
      };

      service.create(request).subscribe((result) => {
        expect(result).toEqual(mockClient);
      });

      const req = httpTesting.expectOne('/api/clients');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockClient);
    });
  });

  describe('update()', () => {
    it('should PUT /api/clients/{id} with request body and return updated client', () => {
      const id = 'client-1';
      const request: UpdateClientRequest = {
        firstName: 'Anna',
        lastName: 'Bakker',
        email: 'anna.bakker@example.com',
        phoneNumber: null,
        notes: null,
      };
      const updatedClient: ClientResponse = { ...mockClient, email: 'anna.bakker@example.com' };

      service.update(id, request).subscribe((result) => {
        expect(result).toEqual(updatedClient);
      });

      const req = httpTesting.expectOne(`/api/clients/${id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(updatedClient);
    });
  });

  describe('delete()', () => {
    it('should DELETE /api/clients/{id}', () => {
      const id = 'client-1';
      let completed = false;

      service.delete(id).subscribe(() => {
        completed = true;
      });

      const req = httpTesting.expectOne(`/api/clients/${id}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      expect(completed).toBe(true);
    });
  });
});
