export interface SubmitDemoRequestPayload {
  contactName: string;
  salonName: string;
  email: string;
  phoneNumber: string | null;
  message: string | null;
}

export interface DemoRequestResponse {
  id: string;
  contactName: string;
  salonName: string;
  email: string;
  createdAtUtc: string;
}
