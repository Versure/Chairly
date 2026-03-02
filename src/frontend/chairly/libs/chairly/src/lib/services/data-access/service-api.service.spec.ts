import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateServiceRequest,
  ServiceResponse,
  UpdateServiceRequest,
} from '../util';
import { ServiceApiService } from './service-api.service';

describe('ServiceApiService', () => {
  let service: ServiceApiService;
  let httpTesting: HttpTestingController;

  const mockService: ServiceResponse = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    name: "Men's Haircut",
    description: null,
    duration: '00:30:00',
    price: 25,
    categoryId: null,
    categoryName: null,
    isActive: true,
    sortOrder: 1,
    createdAtUtc: '2026-01-01T00:00:00Z',
    createdBy: 'admin',
    updatedAtUtc: null,
    updatedBy: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(ServiceApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll()', () => {
    it('should GET /api/services and return services', () => {
      const mockServices: ServiceResponse[] = [mockService];

      service.getAll().subscribe((services) => {
        expect(services).toEqual(mockServices);
      });

      const req = httpTesting.expectOne('/api/services');
      expect(req.request.method).toBe('GET');
      req.flush(mockServices);
    });
  });

  describe('getById()', () => {
    it('should GET /api/services/{id} and return a single service', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';

      service.getById(id).subscribe((result) => {
        expect(result).toEqual(mockService);
      });

      const req = httpTesting.expectOne(`/api/services/${id}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockService);
    });
  });

  describe('create()', () => {
    it('should POST /api/services with request body and return created service', () => {
      const request: CreateServiceRequest = {
        name: "Men's Haircut",
        description: null,
        duration: '00:30:00',
        price: 25,
        categoryId: null,
        sortOrder: 1,
      };

      service.create(request).subscribe((result) => {
        expect(result).toEqual(mockService);
      });

      const req = httpTesting.expectOne('/api/services');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockService);
    });
  });

  describe('update()', () => {
    it('should PUT /api/services/{id} with request body and return updated service', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      const request: UpdateServiceRequest = {
        name: "Updated Men's Haircut",
        description: 'A classic cut',
        duration: '00:45:00',
        price: 30,
        categoryId: null,
        sortOrder: 2,
      };
      const updatedService: ServiceResponse = {
        ...mockService,
        name: "Updated Men's Haircut",
        description: 'A classic cut',
        duration: '00:45:00',
        price: 30,
        sortOrder: 2,
      };

      service.update(id, request).subscribe((result) => {
        expect(result).toEqual(updatedService);
      });

      const req = httpTesting.expectOne(`/api/services/${id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(updatedService);
    });
  });

  describe('delete()', () => {
    it('should DELETE /api/services/{id}', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      let completed = false;

      service.delete(id).subscribe(() => {
        completed = true;
      });

      const req = httpTesting.expectOne(`/api/services/${id}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      expect(completed).toBe(true);
    });
  });

  describe('toggleActive()', () => {
    it('should PATCH /api/services/{id}/toggle-active and return updated service', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      const toggledService: ServiceResponse = { ...mockService, isActive: false };

      service.toggleActive(id).subscribe((result) => {
        expect(result).toEqual(toggledService);
      });

      const req = httpTesting.expectOne(`/api/services/${id}/toggle-active`);
      expect(req.request.method).toBe('PATCH');
      req.flush(toggledService);
    });
  });
});
