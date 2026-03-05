import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { StaffMemberResponse } from '../models';
import { StaffApiService } from './staff-api.service';

export interface StaffState {
  staffMembers: StaffMemberResponse[];
  isLoading: boolean;
  error: string | null;
}

const initialState: StaffState = {
  staffMembers: [],
  isLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export const StaffStore = signalStore(
  withState<StaffState>(initialState),
  withMethods((store) => {
    const staffApi = inject(StaffApiService);

    return {
      loadAll(): void {
        patchState(store, { isLoading: true, error: null });
        staffApi
          .getAll()
          .pipe(take(1))
          .subscribe({
            next: (staffMembers) =>
              patchState(store, { staffMembers, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      addStaffMember(member: StaffMemberResponse): void {
        patchState(store, (state) => ({
          staffMembers: [...state.staffMembers, member],
        }));
      },

      updateStaffMember(member: StaffMemberResponse): void {
        patchState(store, (state) => ({
          staffMembers: state.staffMembers.map((m) =>
            m.id === member.id ? member : m
          ),
        }));
      },

      deactivateStaffMember(id: string): void {
        patchState(store, (state) => ({
          staffMembers: state.staffMembers.map((m) =>
            m.id === id ? { ...m, isActive: false } : m
          ),
        }));
      },

      reactivateStaffMember(id: string): void {
        patchState(store, (state) => ({
          staffMembers: state.staffMembers.map((m) =>
            m.id === id ? { ...m, isActive: true } : m
          ),
        }));
      },
    };
  })
);
