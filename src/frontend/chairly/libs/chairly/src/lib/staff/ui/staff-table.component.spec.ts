import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StaffMemberResponse } from '../models';
import { StaffTableComponent } from './staff-table.component';

const mockActiveStaff: StaffMemberResponse = {
  id: '1',
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

const mockManagerStaff: StaffMemberResponse = {
  id: '2',
  firstName: 'Petra',
  lastName: 'de Vries',
  role: 'manager',
  color: '#8b5cf6',
  photoUrl: null,
  isActive: true,
  schedule: {},
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

const mockInactiveStaff: StaffMemberResponse = {
  id: '3',
  firstName: 'Kees',
  lastName: 'Bakker',
  role: 'staff_member',
  color: '#ef4444',
  photoUrl: null,
  isActive: false,
  schedule: {},
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

describe('StaffTableComponent', () => {
  let component: StaffTableComponent;
  let fixture: ComponentFixture<StaffTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffTableComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StaffTableComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('staffMembers', [mockActiveStaff, mockManagerStaff, mockInactiveStaff]);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render staff member rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    expect(rows.length).toBe(3);
  });

  it('should display full name', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Jan Jansen');
  });

  it('should show "Medewerker" for staff_member role', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Medewerker');
  });

  it('should show "Manager" for manager role', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    expect(rows[1].textContent).toContain('Manager');
  });

  it('should show active badge when isActive is true', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Actief');
    const badge = firstRow.querySelector('.bg-green-100') as HTMLElement | null;
    expect(badge).toBeTruthy();
  });

  it('should show inactive badge when isActive is false', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    const inactiveRow = rows[2];
    expect(inactiveRow.textContent).toContain('Inactief');
    const badge = inactiveRow.querySelector('.bg-gray-100') as HTMLElement | null;
    expect(badge).toBeTruthy();
  });

  it('should show "Deactiveren" button when isActive is true', () => {
    const deactivateBtn = fixture.nativeElement.querySelector('[title="Medewerker deactiveren"]') as HTMLButtonElement | null;
    expect(deactivateBtn).toBeTruthy();
    expect(deactivateBtn?.textContent?.trim()).toBe('Deactiveren');
  });

  it('should show "Activeren" button when isActive is false', () => {
    const activateBtn = fixture.nativeElement.querySelector('[title="Medewerker activeren"]') as HTMLButtonElement | null;
    expect(activateBtn).toBeTruthy();
    expect(activateBtn?.textContent?.trim()).toBe('Activeren');
  });

  it('should emit edit event on Bewerken click', () => {
    let emitted: StaffMemberResponse | undefined;
    component.edit.subscribe((m) => {
      emitted = m;
    });

    const editButton = fixture.nativeElement.querySelector('[title="Medewerker bewerken"]') as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should emit deactivate event on Deactiveren click', () => {
    let emitted: StaffMemberResponse | undefined;
    component.deactivate.subscribe((m) => {
      emitted = m;
    });

    const deactivateButton = fixture.nativeElement.querySelector('[title="Medewerker deactiveren"]') as HTMLButtonElement;
    deactivateButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should emit reactivate event on Activeren click', () => {
    let emitted: StaffMemberResponse | undefined;
    component.reactivate.subscribe((m) => {
      emitted = m;
    });

    const activateButton = fixture.nativeElement.querySelector('[title="Medewerker activeren"]') as HTMLButtonElement;
    activateButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('3');
  });

  it('should show empty state when list is empty', () => {
    fixture.componentRef.setInput('staffMembers', []);
    fixture.detectChanges();

    const emptyCell = fixture.nativeElement.querySelector('td[colspan="5"]') as HTMLTableCellElement | null;
    expect(emptyCell).toBeTruthy();
    expect(emptyCell?.textContent).toContain('Geen medewerkers gevonden');
  });

  it('should apply opacity-60 to inactive rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    expect(rows[2].classList.contains('opacity-60')).toBe(true);
  });

  it('should not apply opacity-60 to active rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    expect(rows[0].classList.contains('opacity-60')).toBe(false);
  });
});
