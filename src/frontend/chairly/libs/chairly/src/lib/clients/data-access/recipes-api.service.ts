import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { ClientRecipeSummary, CreateRecipeRequest, Recipe, UpdateRecipeRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class RecipesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getRecipeByBooking(bookingId: string): Observable<Recipe> {
    return this.http.get<Recipe>(`${this.baseUrl}/recipes/booking/${bookingId}`);
  }

  getClientRecipes(clientId: string): Observable<ClientRecipeSummary[]> {
    return this.http.get<ClientRecipeSummary[]>(`${this.baseUrl}/clients/${clientId}/recipes`);
  }

  createRecipe(request: CreateRecipeRequest): Observable<Recipe> {
    return this.http.post<Recipe>(`${this.baseUrl}/recipes`, request);
  }

  updateRecipe(id: string, request: UpdateRecipeRequest): Observable<Recipe> {
    return this.http.put<Recipe>(`${this.baseUrl}/recipes/${id}`, request);
  }
}
