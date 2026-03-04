export interface ServiceResponse {
  id: string;
  name: string;
  description: string | null;
  duration: string;
  price: number;
  categoryId: string | null;
  categoryName: string | null;
  isActive: boolean;
  sortOrder: number;
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc: string | null;
  updatedBy: string | null;
}

export interface CreateServiceRequest {
  name: string;
  description: string | null;
  duration: string;
  price: number;
  categoryId: string | null;
  sortOrder: number;
}

export interface UpdateServiceRequest {
  name: string;
  description: string | null;
  duration: string;
  price: number;
  categoryId: string | null;
  sortOrder: number;
}
