export interface RecipeProduct {
  id?: string;
  name: string;
  brand?: string;
  quantity?: string;
  sortOrder: number;
}

export interface Recipe {
  id: string;
  bookingId: string;
  clientId: string;
  staffMemberId: string;
  title: string;
  notes?: string;
  products: RecipeProduct[];
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc?: string;
  updatedBy?: string;
}

export interface ClientRecipeSummary {
  id: string;
  bookingId: string;
  bookingDate: string;
  staffMemberId: string;
  staffMemberName: string;
  title: string;
  notes?: string;
  products: RecipeProduct[];
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateRecipeRequest {
  bookingId: string;
  title: string;
  notes?: string;
  products: Omit<RecipeProduct, 'id'>[];
}

export interface UpdateRecipeRequest {
  title: string;
  notes?: string;
  products: Omit<RecipeProduct, 'id'>[];
}
