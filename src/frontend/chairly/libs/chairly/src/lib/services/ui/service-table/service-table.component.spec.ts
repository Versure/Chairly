import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';
import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { ServiceResponse } from '../../models';
import { ServiceTableComponent } from './service-table.component';

registerLocaleData(localeNl);

const mockActiveService: ServiceResponse = {
  id: '1',
  name: "Men's Haircut",
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
      providers: [
        { provide: LOCALE_ID, useValue: 'nl-NL' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
      ],
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
    const rows = fixture.nativeElement.querySelectorAll(
      'tbody tr',
    ) as NodeListOf<HTMLTableRowElement>;
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
    const rows = fixture.nativeElement.querySelectorAll(
      'tbody tr',
    ) as NodeListOf<HTMLTableRowElement>;
    expect(rows[1].textContent).toContain('—');
  });

  it('should display duration via DurationPipe', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('30 min');
  });

  it('should format price using Dutch (nl-NL) locale', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    // Dutch locale formats EUR currency with comma as decimal separator, e.g. "€ 25,00"
    expect(firstRow.textContent).toMatch(/€\s*25[,.]00/);
  });

  it('should show active badge for active services', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Actief');
    const badge = firstRow.querySelector('.bg-green-100') as HTMLElement | null;
    expect(badge).toBeTruthy();
  });

  it('should show inactive badge for inactive services', () => {
    const rows = fixture.nativeElement.querySelectorAll(
      'tbody tr',
    ) as NodeListOf<HTMLTableRowElement>;
    const secondRow = rows[1];
    expect(secondRow.textContent).toContain('Inactief');
    const badge = secondRow.querySelector('.bg-gray-100') as HTMLElement | null;
    expect(badge).toBeTruthy();
  });

  it('should show loading indicator when isLoading is true', () => {
    fixture.componentRef.setInput('isLoading', true);
    fixture.detectChanges();

    const indicator = fixture.nativeElement.querySelector(
      'chairly-loading-indicator',
    ) as HTMLElement | null;
    expect(indicator).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Diensten laden...');
    const table = fixture.nativeElement.querySelector('table') as HTMLTableElement | null;
    expect(table).toBeNull();
  });

  it('should show empty state when no services exist', () => {
    fixture.componentRef.setInput('services', []);
    fixture.detectChanges();

    const emptyCell = fixture.nativeElement.querySelector(
      'td[colspan="6"]',
    ) as HTMLTableCellElement | null;
    expect(emptyCell).toBeTruthy();
    expect(emptyCell?.textContent).toContain('Nog geen diensten.');
  });

  it('should emit editClicked when Edit button is clicked', () => {
    let emitted: ServiceResponse | undefined;
    component.editClicked.subscribe((s) => {
      emitted = s;
    });

    const editButton = fixture.nativeElement.querySelector(
      '[title="Dienst bewerken"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should emit deleteClicked when Delete button is clicked', () => {
    let emitted: ServiceResponse | undefined;
    component.deleteClicked.subscribe((s) => {
      emitted = s;
    });

    const deleteButton = fixture.nativeElement.querySelector(
      '[title="Dienst verwijderen"]',
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should emit toggleActiveClicked when Toggle Active button is clicked', () => {
    let emitted: ServiceResponse | undefined;
    component.toggleActiveClicked.subscribe((s) => {
      emitted = s;
    });

    const toggleButton = fixture.nativeElement.querySelector(
      '[title="Status wijzigen"]',
    ) as HTMLButtonElement;
    toggleButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('1');
  });

  it('should show Deactivate for active services and Activate for inactive services', () => {
    const toggleButtons = fixture.nativeElement.querySelectorAll(
      '[title="Status wijzigen"]',
    ) as NodeListOf<HTMLButtonElement>;
    expect(toggleButtons[0].textContent?.trim()).toBe('Deactiveren');
    expect(toggleButtons[1].textContent?.trim()).toBe('Activeren');
  });

  it('should emit servicesReordered when a row is dropped on another row', () => {
    let reordered: ServiceResponse[] | undefined;
    component.servicesReordered.subscribe((s) => {
      reordered = s;
    });

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    rows[0].triggerEventHandler('dragstart', null);
    rows[1].triggerEventHandler('dragover', { preventDefault: vi.fn() });
    rows[1].triggerEventHandler('drop', null);
    fixture.detectChanges();

    expect(reordered).toBeDefined();
    const result = reordered as ServiceResponse[];
    expect(result[0].id).toBe('2');
    expect(result[1].id).toBe('1');
  });
});
