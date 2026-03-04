import { TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import {
  CreateServiceCategoryRequest,
  ServiceCategoryResponse,
  UpdateServiceCategoryRequest,
} from '../models';
import { ServiceCategoryStore } from './service-category.store';
import { ServiceCategoryApiService } from './service-category-api.service';

describe('ServiceCategoryStore', () => {
  const mockCategory: ServiceCategoryResponse = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    name: 'Haircuts',
    sortOrder: 1,
    createdAtUtc: '2026-01-01T00:00:00Z',
    createdBy: 'admin',
  };

  const mockCategoryService = {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  };

  let store: InstanceType<typeof ServiceCategoryStore>;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [
        ServiceCategoryStore,
        { provide: ServiceCategoryApiService, useValue: mockCategoryService },
      ],
    });

    store = TestBed.inject(ServiceCategoryStore);
  });

  it('should initialize with empty state', () => {
    expect(store.categories()).toEqual([]);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  describe('loadCategories()', () => {
    it('should update categories and clear isLoading on success', () => {
      const categories = [mockCategory];
      mockCategoryService.getAll.mockReturnValue(of(categories));

      store.loadCategories();

      expect(store.categories()).toEqual(categories);
      expect(store.isLoading()).toBe(false);
      expect(store.error()).toBeNull();
    });

    it('should set error and clear isLoading on failure', () => {
      const error = new Error('Network error');
      mockCategoryService.getAll.mockReturnValue(throwError(() => error));

      store.loadCategories();

      expect(store.error()).toBe('Network error');
      expect(store.isLoading()).toBe(false);
    });
  });

  describe('createCategory()', () => {
    it('should add category to categories array on success', () => {
      const request: CreateServiceCategoryRequest = {
        name: 'Haircuts',
        sortOrder: 1,
      };
      mockCategoryService.create.mockReturnValue(of(mockCategory));

      store.createCategory(request);

      expect(store.categories()).toEqual([mockCategory]);
    });

    it('should set error on failure', () => {
      const request: CreateServiceCategoryRequest = {
        name: 'Haircuts',
        sortOrder: 1,
      };
      const error = new Error('Server error');
      mockCategoryService.create.mockReturnValue(throwError(() => error));

      store.createCategory(request);

      expect(store.error()).toBe('Server error');
    });
  });

  describe('updateCategory()', () => {
    it('should replace category in array on success', () => {
      mockCategoryService.getAll.mockReturnValue(of([mockCategory]));
      store.loadCategories();

      const updatedCategory: ServiceCategoryResponse = {
        ...mockCategory,
        name: 'Updated Haircuts',
      };
      const request: UpdateServiceCategoryRequest = {
        name: 'Updated Haircuts',
        sortOrder: 1,
      };
      mockCategoryService.update.mockReturnValue(of(updatedCategory));

      store.updateCategory(mockCategory.id, request);

      expect(store.categories()).toEqual([updatedCategory]);
    });

    it('should set error on failure', () => {
      const request: UpdateServiceCategoryRequest = {
        name: 'Updated',
        sortOrder: 1,
      };
      const error = new Error('Update failed');
      mockCategoryService.update.mockReturnValue(throwError(() => error));

      store.updateCategory(mockCategory.id, request);

      expect(store.error()).toBe('Update failed');
    });
  });

  describe('deleteCategory()', () => {
    it('should remove category from array on success', () => {
      mockCategoryService.getAll.mockReturnValue(of([mockCategory]));
      store.loadCategories();

      mockCategoryService.delete.mockReturnValue(of(null));
      store.deleteCategory(mockCategory.id);

      expect(store.categories()).toEqual([]);
    });

    it('should set error on failure', () => {
      const error = new Error('Delete failed');
      mockCategoryService.delete.mockReturnValue(throwError(() => error));

      store.deleteCategory(mockCategory.id);

      expect(store.error()).toBe('Delete failed');
    });
  });

  describe('reorderCategories()', () => {
    it('should optimistically reorder categories with new sortOrder values', () => {
      const cat1: ServiceCategoryResponse = { ...mockCategory, id: 'a', name: 'Alpha', sortOrder: 0 };
      const cat2: ServiceCategoryResponse = { ...mockCategory, id: 'b', name: 'Beta', sortOrder: 1 };
      mockCategoryService.getAll.mockReturnValue(of([cat1, cat2]));
      store.loadCategories();

      mockCategoryService.update.mockReturnValue(of({ ...cat1, sortOrder: 1 }));

      store.reorderCategories([cat2, cat1]);

      expect(store.categories()[0].id).toBe('b');
      expect(store.categories()[0].sortOrder).toBe(0);
      expect(store.categories()[1].id).toBe('a');
      expect(store.categories()[1].sortOrder).toBe(1);
    });

    it('should call update API for each category with new sortOrder', () => {
      const cat1: ServiceCategoryResponse = { ...mockCategory, id: 'a', name: 'Alpha', sortOrder: 0 };
      const cat2: ServiceCategoryResponse = { ...mockCategory, id: 'b', name: 'Beta', sortOrder: 1 };
      mockCategoryService.update.mockReturnValue(of(cat1));

      store.reorderCategories([cat2, cat1]);

      expect(mockCategoryService.update).toHaveBeenCalledWith('b', { name: 'Beta', sortOrder: 0 });
      expect(mockCategoryService.update).toHaveBeenCalledWith('a', { name: 'Alpha', sortOrder: 1 });
    });
  });
});
