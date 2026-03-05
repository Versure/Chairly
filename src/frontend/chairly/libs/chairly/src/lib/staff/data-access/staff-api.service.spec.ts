import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateStaffMemberRequest,
  StaffMemberResponse,
  UpdateStaffMemberRequest,
} from '../models';
import { StaffApiService } from './staff-api.service';

describe('StaffApiService', () => {
  let service: StaffApiService;
  let httpTesting: HttpTestingController;

  const mockStaffMember: StaffMemberResponse = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    firstName: 'Jan',
    lastName: 'de Vries',
    role: 'staff_member',
    color: '#6366f1',
    photoUrl: null,
    isActive: true,
    schedule: {},
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api' },
      ],
    });

    service = TestBed.inject(StaffApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll()', () => {
    it('should GET /api/staff and return staff members', () => {
      const mockList: StaffMemberResponse[] = [mockStaffMember];

      service.getAll().subscribe((members) => {
        expect(members).toEqual(mockList);
      });

      const req = httpTesting.expectOne('/api/staff');
      expect(req.request.method).toBe('GET');
      req.flush(mockList);
    });
  });

  describe('create()', () => {
    it('should POST /api/staff with request body and return created staff member', () => {
      const request: CreateStaffMemberRequest = {
        firstName: 'Jan',
        lastName: 'de Vries',
        role: 'staff_member',
        color: '#6366f1',
        photoUrl: null,
        schedule: {},
      };

      service.create(request).subscribe((result) => {
        expect(result).toEqual(mockStaffMember);
      });

      const req = httpTesting.expectOne('/api/staff');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockStaffMember);
    });
  });

  describe('update()', () => {
    it('should PUT /api/staff/{id} with request body and return updated staff member', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      const request: UpdateStaffMemberRequest = {
        firstName: 'Jan',
        lastName: 'de Vries',
        role: 'manager',
        color: '#8b5cf6',
        photoUrl: null,
        schedule: {},
      };
      const updatedMember: StaffMemberResponse = { ...mockStaffMember, role: 'manager', color: '#8b5cf6' };

      service.update(id, request).subscribe((result) => {
        expect(result).toEqual(updatedMember);
      });

      const req = httpTesting.expectOne(`/api/staff/${id}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(updatedMember);
    });
  });

  describe('deactivate()', () => {
    it('should PATCH /api/staff/{id}/deactivate', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      let completed = false;

      service.deactivate(id).subscribe(() => {
        completed = true;
      });

      const req = httpTesting.expectOne(`/api/staff/${id}/deactivate`);
      expect(req.request.method).toBe('PATCH');
      req.flush(null);

      expect(completed).toBe(true);
    });
  });

  describe('reactivate()', () => {
    it('should PATCH /api/staff/{id}/reactivate', () => {
      const id = '123e4567-e89b-12d3-a456-426614174000';
      let completed = false;

      service.reactivate(id).subscribe(() => {
        completed = true;
      });

      const req = httpTesting.expectOne(`/api/staff/${id}/reactivate`);
      expect(req.request.method).toBe('PATCH');
      req.flush(null);

      expect(completed).toBe(true);
    });
  });
});
