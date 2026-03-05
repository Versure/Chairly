import { TestBed } from '@angular/core/testing';

import { of, throwError } from 'rxjs';

import { StaffMemberResponse } from '../models';
import { StaffStore } from './staff.store';
import { StaffApiService } from './staff-api.service';

describe('StaffStore', () => {
  const mockMember: StaffMemberResponse = {
    id: 'staff-001',
    firstName: 'Jan',
    lastName: 'Jansen',
    role: 'staff_member',
    color: '#6366f1',
    photoUrl: null,
    isActive: true,
    schedule: {},
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  };

  const mockApiService = {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    deactivate: vi.fn(),
    reactivate: vi.fn(),
  };

  let store: InstanceType<typeof StaffStore>;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      providers: [
        StaffStore,
        { provide: StaffApiService, useValue: mockApiService },
      ],
    });

    store = TestBed.inject(StaffStore);
  });

  it('should initialize with empty state and not loading', () => {
    expect(store.staffMembers()).toEqual([]);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  describe('loadAll()', () => {
    it('should set staffMembers and clear isLoading on success', () => {
      mockApiService.getAll.mockReturnValue(of([mockMember]));

      store.loadAll();

      expect(store.staffMembers()).toEqual([mockMember]);
      expect(store.isLoading()).toBe(false);
      expect(store.error()).toBeNull();
    });

    it('should set error and clear isLoading on failure', () => {
      const error = new Error('Network error');
      mockApiService.getAll.mockReturnValue(throwError(() => error));

      store.loadAll();

      expect(store.error()).toBe('Network error');
      expect(store.isLoading()).toBe(false);
    });
  });

  describe('addStaffMember()', () => {
    it('should append member to staffMembers array', () => {
      const newMember: StaffMemberResponse = {
        ...mockMember,
        id: 'staff-002',
        firstName: 'Piet',
        lastName: 'Peters',
      };

      store.addStaffMember(newMember);

      expect(store.staffMembers()).toEqual([newMember]);
    });

    it('should append to existing members', () => {
      mockApiService.getAll.mockReturnValue(of([mockMember]));
      store.loadAll();

      const newMember: StaffMemberResponse = {
        ...mockMember,
        id: 'staff-002',
        firstName: 'Piet',
      };
      store.addStaffMember(newMember);

      expect(store.staffMembers()).toHaveLength(2);
      expect(store.staffMembers()[1]).toEqual(newMember);
    });
  });

  describe('updateStaffMember()', () => {
    it('should replace member by id', () => {
      mockApiService.getAll.mockReturnValue(of([mockMember]));
      store.loadAll();

      const updated: StaffMemberResponse = { ...mockMember, firstName: 'Johan' };
      store.updateStaffMember(updated);

      expect(store.staffMembers()).toEqual([updated]);
    });

    it('should not affect other members', () => {
      const member2: StaffMemberResponse = { ...mockMember, id: 'staff-002', firstName: 'Piet' };
      mockApiService.getAll.mockReturnValue(of([mockMember, member2]));
      store.loadAll();

      const updated: StaffMemberResponse = { ...mockMember, firstName: 'Johan' };
      store.updateStaffMember(updated);

      expect(store.staffMembers()[0].firstName).toBe('Johan');
      expect(store.staffMembers()[1].firstName).toBe('Piet');
    });
  });

  describe('deactivateStaffMember()', () => {
    it('should set isActive to false for matching member', () => {
      mockApiService.getAll.mockReturnValue(of([mockMember]));
      store.loadAll();

      store.deactivateStaffMember(mockMember.id);

      expect(store.staffMembers()[0].isActive).toBe(false);
    });

    it('should not affect other members', () => {
      const member2: StaffMemberResponse = { ...mockMember, id: 'staff-002', isActive: true };
      mockApiService.getAll.mockReturnValue(of([mockMember, member2]));
      store.loadAll();

      store.deactivateStaffMember(mockMember.id);

      expect(store.staffMembers()[0].isActive).toBe(false);
      expect(store.staffMembers()[1].isActive).toBe(true);
    });
  });

  describe('reactivateStaffMember()', () => {
    it('should set isActive to true for matching member', () => {
      const inactiveMember: StaffMemberResponse = { ...mockMember, isActive: false };
      mockApiService.getAll.mockReturnValue(of([inactiveMember]));
      store.loadAll();

      store.reactivateStaffMember(inactiveMember.id);

      expect(store.staffMembers()[0].isActive).toBe(true);
    });
  });
});
