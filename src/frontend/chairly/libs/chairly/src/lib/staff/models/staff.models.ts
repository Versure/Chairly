export type StaffRole = 'manager' | 'staff_member';

export interface ShiftBlock {
  startTime: string;
  endTime: string;
}

export type DayOfWeek =
  | 'monday'
  | 'tuesday'
  | 'wednesday'
  | 'thursday'
  | 'friday'
  | 'saturday'
  | 'sunday';

export type WeeklySchedule = Partial<Record<DayOfWeek, ShiftBlock[]>>;

export interface StaffMemberResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: StaffRole;
  color: string;
  photoUrl: string | null;
  isActive: boolean;
  schedule: WeeklySchedule;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface CreateStaffMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: StaffRole;
  color: string;
  photoUrl: string | null;
  schedule: WeeklySchedule;
}

export type UpdateStaffMemberRequest = CreateStaffMemberRequest;
