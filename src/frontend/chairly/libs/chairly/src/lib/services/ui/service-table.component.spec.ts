import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceResponse } from '../util';
import { ServiceTableComponent } from './service-table.component';

const mockActiveService: ServiceResponse = {
  id: '1',
  name: 'Men\'s Haircut',
  description: 'A classic haircut',
  duration: '00:30:00',
  price: 25,
  categoryId: 'cat1',
  categoryName: 'Hair',
  isActive: true,
  sortOrder: 1,
  createdAtUtc: '2026-01-01T00:00:00Z',
  createdBy: 'admin',
  updatedAtUtc: null,
  updatedBy: null,
};

const mockInactiveService: ServiceResponse = {
  id: '2',
  name: 'Beard Trim',
  description: null,
  duration: '01:00:00',
  price: 15,
  categoryId: null,
  categoryName: null,
  isActive: false,
  sortOrder: 2,
  createdAtUtc: '2026-01-01T00:00:00Z',
  createdBy: 'admin',
  updatedAtUtc: null,
  updatedBy: null,
};

describe('ServiceTableComponent', () => {
  let component: ServiceTableComponent;
  let fixture: ComponentFixture<ServiceTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceTableComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceTableComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('services', [mockActiveService, mockInactiveService]);
    fixture.componentRef.setInput('isLoading', false);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display service rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    expect(rows.length).toBe(2);
  });

  it('should display service name', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain("Men's Haircut");
  });

  it('should display category name when set', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Hair');
  });

  it('should display em dash when categoryName is null', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    expect(rows[1].textContent).toContain('—');
  });

  it('should display duration via DurationPipe', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('30 min');
  });

  it('should show active badge for active services', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Active');
    const badge = firstRow.querySelector('.bg-green-100') as HTMLElement | null;
    expect(badge).toBeTruthy();
  });

  it('should show inactive badge for inactive services', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;
    const secondRow = rows[1];
    expect(secondRow.textContent).toContain('Inactive');
    const badge = secondRow.querySelector('.bg-gray-100') as HTMLElement | null;
    expect(badge).toBeTruthy();
  });

  it('should show loading indicator when isLoading is true', () => {
    fixture.componentRef.setInput('isLoading', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Loading services...');
    const table = fixture.nativeElement.querySelector('table') as HTMLTableElement | null;
    expect(table).toBeNull();
  });

  it('should show empty state when no services exist', () => {
    fixture.componentRef.setInput('services', []);
    fixture.detectChanges();

    const emptyCell = fixture.nativeElement.querySelector('td[colspan="6"]') as HTMLTableCellElement | null;
    expect(emptyCell).toBeTruthy();
    expect(emptyCell?.textContent).toContain('No services yet.');
  });

  it('should emit editClicked when Edit button is clicked', () => {
    let emitted: ServiceResponse | undefined;
    component.editClicked.subscribe((s) => {
      emitted = s;
    });

    const editButton = fixture.nativeElement.querySelector('[title="Edit service"]') as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should emit deleteClicked when Delete button is clicked', () => {
    let emitted: ServiceResponse | undefined;
    component.deleteClicked.subscribe((s) => {
      emitted = s;
    });

    const deleteButton = fixture.nativeElement.querySelector('[title="Delete service"]') as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should emit toggleActiveClicked when Toggle Active button is clicked', () => {
    let emitted: ServiceResponse | undefined;
    component.toggleActiveClicked.subscribe((s) => {
      emitted = s;
    });

    const toggleButton = fixture.nativeElement.querySelector('[title="Toggle active"]') as HTMLButtonElement;
    toggleButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should show Deactivate for active services and Activate for inactive services', () => {
    const toggleButtons = fixture.nativeElement.querySelectorAll('[title="Toggle active"]') as NodeListOf<HTMLButtonElement>;
    expect(toggleButtons[0].textContent?.trim()).toBe('Deactivate');
    expect(toggleButtons[1].textContent?.trim()).toBe('Activate');
  });
});
