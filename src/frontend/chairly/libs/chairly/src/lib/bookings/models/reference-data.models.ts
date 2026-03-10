export interface ClientOption {
  id: string;
  firstName: string;
  lastName: string;
}

export interface StaffMemberOption {
  id: string;
  firstName: string;
  lastName: string;
  color: string; // hex color string e.g. "#FF5733"
}

export interface ServiceOption {
  id: string;
  name: string;
  duration: string;
  price: number;
}
