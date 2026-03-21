export interface SubmitSignUpRequestPayload {
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  phoneNumber: string | null;
}

export interface SignUpRequestResponse {
  id: string;
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  createdAtUtc: string;
}
