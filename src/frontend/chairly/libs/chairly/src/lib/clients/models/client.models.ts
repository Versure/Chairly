export interface ClientResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface CreateClientRequest {
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  notes: string | null;
}

export type UpdateClientRequest = CreateClientRequest;
