import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateServiceCategoryRequest,
  ServiceCategoryResponse,
  UpdateServiceCategoryRequest,
} from '../models';
import { ServiceCategoryApiService } from './service-category-api.service';

describe('ServiceCategoryApiService', () => {
  let service: ServiceCategoryApiService;
  let httpTesting: HttpTestingController;

  const mockCategory: ServiceCategoryResponse = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    name: 'Haircuts',
    sortOrder: 1,
    createdAtUtc: '2026-01-01T00:00:00Z',
    createdBy: 'admin',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(ServiceCategoryApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll()', () => {
    it('should GET /api/service-categories and return categories', () => {
      const mockCategories: ServiceCategoryResponse[] = [mockCategory];

      service.getAll().subscribe((categories) => {
        expect(categories).toEqual(mockCategories);
      });

      const req = httpTesting.expectOne('/api/service-categories');
      expect(req.request.method).toBe('GET');
      req.flush(mockCategories);
    });
  });

  describe('create()', () => {
    it('should POST /api/service-categories with request body and return created category', () => {
      const request: CreateServiceCategoryRequest = {
        name: 'Haircuts',
        sortOrder: 1,
      };

      service.create(request).subscribe((category) => {
        expect(category).toEqual(mockCategory);
      });

      const req = httpTesting.expectOne('/api/service-categories');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockCategory);
    });
  });

  describe('update()', () => {
    it('should PUT /api/service-categories/{id} with request body and return updated category', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      const request: UpdateServiceCategoryRequest = {
        name: 'Updated Haircuts',
        sortOrder: 2,
      };
      const updatedCategory: ServiceCategoryResponse = {
        ...mockCategory,
        name: 'Updated Haircuts',
        sortOrder: 2,
      };

      service.update(id, request).subscribe((category) => {
        expect(category).toEqual(updatedCategory);
      });

      const req = httpTesting.expectOne(`/api/service-categories/${id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(updatedCategory);
    });
  });

  describe('delete()', () => {
    it('should DELETE /api/service-categories/{id}', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      let completed = false;

      service.delete(id).subscribe(() => {
        completed = true;
      });

      const req = httpTesting.expectOne(`/api/service-categories/${id}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      expect(completed).toBe(true);
    });
  });
});
