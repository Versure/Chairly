import { TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import { CreateServiceRequest, ServiceResponse, UpdateServiceRequest } from '../models';
import { ServiceStore } from './service.store';
import { ServiceApiService } from './service-api.service';

describe('ServiceStore', () => {
  const mockService: ServiceResponse = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    name: "Men's Haircut",
    description: null,
    duration: '00:30:00',
    price: 25,
    vatRate: 21,
    categoryId: 'cat-001',
    categoryName: 'Haircuts',
    isActive: true,
    sortOrder: 1,
    createdAtUtc: '2026-01-01T00:00:00Z',
    createdBy: 'admin',
    updatedAtUtc: null,
    updatedBy: null,
  };

  const mockInactiveService: ServiceResponse = {
    ...mockService,
    id: '223e4567-e89b-12d3-a456-426614174001',
    name: 'Beard Trim',
    isActive: false,
    categoryId: 'cat-002',
    categoryName: 'Beard',
  };

  const mockUncategorizedService: ServiceResponse = {
    ...mockService,
    id: '323e4567-e89b-12d3-a456-426614174002',
    name: 'Custom Style',
    categoryId: null,
    categoryName: null,
  };

  const mockApiService = {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    toggleActive: vi.fn(),
  };

  let store: InstanceType<typeof ServiceStore>;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [ServiceStore, { provide: ServiceApiService, useValue: mockApiService }],
    });

    store = TestBed.inject(ServiceStore);
  });

  it('should initialize with empty state', () => {
    expect(store.services()).toEqual([]);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  describe('activeServices computed', () => {
    it('should return only active services', () => {
      mockApiService.getAll.mockReturnValue(of([mockService, mockInactiveService]));
      store.loadServices();

      expect(store.activeServices()).toEqual([mockService]);
    });

    it('should return empty array when no active services', () => {
      mockApiService.getAll.mockReturnValue(of([mockInactiveService]));
      store.loadServices();

      expect(store.activeServices()).toEqual([]);
    });
  });

  describe('servicesByCategory computed', () => {
    it('should group services by categoryId', () => {
      mockApiService.getAll.mockReturnValue(of([mockService, mockInactiveService]));
      store.loadServices();

      const grouped = store.servicesByCategory();
      expect(grouped['cat-001']).toEqual([mockService]);
      expect(grouped['cat-002']).toEqual([mockInactiveService]);
    });

    it('should use uncategorized key for null categoryId', () => {
      mockApiService.getAll.mockReturnValue(of([mockUncategorizedService]));
      store.loadServices();

      const grouped = store.servicesByCategory();
      expect(grouped['uncategorized']).toEqual([mockUncategorizedService]);
    });

    it('should return empty object for empty services', () => {
      expect(store.servicesByCategory()).toEqual({});
    });
  });

  describe('loadServices()', () => {
    it('should update services and clear isLoading on success', () => {
      const services = [mockService];
      mockApiService.getAll.mockReturnValue(of(services));

      store.loadServices();

      expect(store.services()).toEqual(services);
      expect(store.isLoading()).toBe(false);
      expect(store.error()).toBeNull();
    });

    it('should set error and clear isLoading on failure', () => {
      const error = new Error('Network error');
      mockApiService.getAll.mockReturnValue(throwError(() => error));

      store.loadServices();

      expect(store.error()).toBe('Network error');
      expect(store.isLoading()).toBe(false);
    });
  });

  describe('createService()', () => {
    it('should add service to services array on success', () => {
      const request: CreateServiceRequest = {
        name: "Men's Haircut",
        description: null,
        duration: '00:30:00',
        price: 25,
        vatRate: 21,
        categoryId: 'cat-001',
        sortOrder: 1,
      };
      mockApiService.create.mockReturnValue(of(mockService));

      store.createService(request);

      expect(store.services()).toEqual([mockService]);
    });

    it('should set error on failure', () => {
      const request: CreateServiceRequest = {
        name: "Men's Haircut",
        description: null,
        duration: '00:30:00',
        price: 25,
        vatRate: null,
        categoryId: null,
        sortOrder: 1,
      };
      const error = new Error('Server error');
      mockApiService.create.mockReturnValue(throwError(() => error));

      store.createService(request);

      expect(store.error()).toBe('Server error');
    });
  });

  describe('updateService()', () => {
    it('should replace service in array on success', () => {
      mockApiService.getAll.mockReturnValue(of([mockService]));
      store.loadServices();

      const updatedService: ServiceResponse = {
        ...mockService,
        name: 'Updated Haircut',
      };
      const request: UpdateServiceRequest = {
        name: 'Updated Haircut',
        description: null,
        duration: '00:30:00',
        price: 25,
        vatRate: 21,
        categoryId: 'cat-001',
        sortOrder: 1,
      };
      mockApiService.update.mockReturnValue(of(updatedService));

      store.updateService(mockService.id, request);

      expect(store.services()).toEqual([updatedService]);
    });

    it('should set error on failure', () => {
      const request: UpdateServiceRequest = {
        name: 'Updated',
        description: null,
        duration: '00:30:00',
        price: 25,
        vatRate: null,
        categoryId: null,
        sortOrder: 1,
      };
      const error = new Error('Update failed');
      mockApiService.update.mockReturnValue(throwError(() => error));

      store.updateService(mockService.id, request);

      expect(store.error()).toBe('Update failed');
    });
  });

  describe('deleteService()', () => {
    it('should remove service from array on success', () => {
      mockApiService.getAll.mockReturnValue(of([mockService]));
      store.loadServices();

      mockApiService.delete.mockReturnValue(of(null));
      store.deleteService(mockService.id);

      expect(store.services()).toEqual([]);
    });

    it('should set error on failure', () => {
      const error = new Error('Delete failed');
      mockApiService.delete.mockReturnValue(throwError(() => error));

      store.deleteService(mockService.id);

      expect(store.error()).toBe('Delete failed');
    });
  });

  describe('toggleActive()', () => {
    it('should replace service with toggled version on success', () => {
      mockApiService.getAll.mockReturnValue(of([mockService]));
      store.loadServices();

      const toggled: ServiceResponse = { ...mockService, isActive: false };
      mockApiService.toggleActive.mockReturnValue(of(toggled));

      store.toggleActive(mockService.id);

      expect(store.services()).toEqual([toggled]);
    });

    it('should set error on failure', () => {
      const error = new Error('Toggle failed');
      mockApiService.toggleActive.mockReturnValue(throwError(() => error));

      store.toggleActive(mockService.id);

      expect(store.error()).toBe('Toggle failed');
    });
  });

  describe('reorderServices()', () => {
    it('should optimistically reorder services with new sortOrder values', () => {
      const svc1: ServiceResponse = { ...mockService, id: 'a', name: 'Alpha', sortOrder: 0 };
      const svc2: ServiceResponse = { ...mockService, id: 'b', name: 'Beta', sortOrder: 1 };
      mockApiService.getAll.mockReturnValue(of([svc1, svc2]));
      store.loadServices();

      mockApiService.update.mockReturnValue(of(svc1));

      store.reorderServices([svc2, svc1]);

      expect(store.services()[0].id).toBe('b');
      expect(store.services()[0].sortOrder).toBe(0);
      expect(store.services()[1].id).toBe('a');
      expect(store.services()[1].sortOrder).toBe(1);
    });

    it('should call update API for each service with new sortOrder', () => {
      const svc1: ServiceResponse = { ...mockService, id: 'a', name: 'Alpha', sortOrder: 0 };
      const svc2: ServiceResponse = { ...mockService, id: 'b', name: 'Beta', sortOrder: 1 };
      mockApiService.update.mockReturnValue(of(svc1));

      store.reorderServices([svc2, svc1]);

      expect(mockApiService.update).toHaveBeenCalledWith(
        'b',
        expect.objectContaining({ name: 'Beta', sortOrder: 0 }),
      );
      expect(mockApiService.update).toHaveBeenCalledWith(
        'a',
        expect.objectContaining({ name: 'Alpha', sortOrder: 1 }),
      );
    });
  });
});
