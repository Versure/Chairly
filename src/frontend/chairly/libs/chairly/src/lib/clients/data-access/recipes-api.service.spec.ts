import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import { ClientRecipeSummary, CreateRecipeRequest, Recipe, UpdateRecipeRequest } from '../models';
import { RecipesApiService } from './recipes-api.service';

describe('RecipesApiService', () => {
  let service: RecipesApiService;
  let httpTesting: HttpTestingController;

  const mockRecipe: Recipe = {
    id: 'recipe-1',
    bookingId: 'booking-1',
    clientId: 'client-1',
    staffMemberId: 'staff-1',
    title: 'Volledige kleuring',
    notes: 'Warme tint',
    products: [
      { id: 'prod-1', name: 'Wella Illumina', brand: 'Wella', quantity: '60 ml', sortOrder: 0 },
    ],
    createdAtUtc: '2026-01-15T10:00:00Z',
    createdBy: 'staff-1',
    updatedAtUtc: undefined,
    updatedBy: undefined,
  };

  const mockSummary: ClientRecipeSummary = {
    id: 'recipe-1',
    bookingId: 'booking-1',
    bookingDate: '2026-01-15T10:00:00Z',
    staffMemberId: 'staff-1',
    staffMemberName: 'Jan Jansen',
    title: 'Volledige kleuring',
    notes: 'Warme tint',
    products: [
      { id: 'prod-1', name: 'Wella Illumina', brand: 'Wella', quantity: '60 ml', sortOrder: 0 },
    ],
    createdAtUtc: '2026-01-15T10:00:00Z',
    updatedAtUtc: undefined,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(RecipesApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getRecipeByBooking()', () => {
    it('should GET /api/recipes/booking/{bookingId} and return recipe', () => {
      service.getRecipeByBooking('booking-1').subscribe((recipe) => {
        expect(recipe).toEqual(mockRecipe);
      });

      const req = httpTesting.expectOne('/api/recipes/booking/booking-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockRecipe);
    });
  });

  describe('getClientRecipes()', () => {
    it('should GET /api/clients/{clientId}/recipes and return summaries', () => {
      const mockList: ClientRecipeSummary[] = [mockSummary];

      service.getClientRecipes('client-1').subscribe((recipes) => {
        expect(recipes).toEqual(mockList);
      });

      const req = httpTesting.expectOne('/api/clients/client-1/recipes');
      expect(req.request.method).toBe('GET');
      req.flush(mockList);
    });
  });

  describe('createRecipe()', () => {
    it('should POST /api/recipes with request body and return created recipe', () => {
      const request: CreateRecipeRequest = {
        bookingId: 'booking-1',
        title: 'Volledige kleuring',
        notes: 'Warme tint',
        products: [{ name: 'Wella Illumina', brand: 'Wella', quantity: '60 ml', sortOrder: 0 }],
      };

      service.createRecipe(request).subscribe((result) => {
        expect(result).toEqual(mockRecipe);
      });

      const req = httpTesting.expectOne('/api/recipes');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockRecipe);
    });
  });

  describe('updateRecipe()', () => {
    it('should PUT /api/recipes/{id} with request body and return updated recipe', () => {
      const id = 'recipe-1';
      const request: UpdateRecipeRequest = {
        title: 'Bijgewerkte kleuring',
        notes: 'Koude tint',
        products: [{ name: 'Wella Koleston', brand: 'Wella', quantity: '45 ml', sortOrder: 0 }],
      };
      const updatedRecipe: Recipe = {
        ...mockRecipe,
        title: 'Bijgewerkte kleuring',
        notes: 'Koude tint',
        updatedAtUtc: '2026-01-16T10:00:00Z',
        updatedBy: 'staff-1',
      };

      service.updateRecipe(id, request).subscribe((result) => {
        expect(result).toEqual(updatedRecipe);
      });

      const req = httpTesting.expectOne(`/api/recipes/${id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(updatedRecipe);
    });
  });
});
