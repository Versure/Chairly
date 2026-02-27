export interface ServiceCategoryResponse {
  id: string;
  name: string;
  sortOrder: number;
  createdAtUtc: string;
  createdBy: string;
}

export interface CreateServiceCategoryRequest {
  name: string;
  sortOrder: number;
}

export interface UpdateServiceCategoryRequest {
  name: string;
  sortOrder: number;
}
