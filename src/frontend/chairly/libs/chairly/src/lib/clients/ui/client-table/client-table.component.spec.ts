import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientResponse } from '../../models';
import { ClientTableComponent } from './client-table.component';

const mockClient: ClientResponse = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: null,
  notes: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

const mockClient2: ClientResponse = {
  id: 'client-2',
  firstName: 'Bert',
  lastName: 'Claassen',
  email: null,
  phoneNumber: '+31612345678',
  notes: null,
  createdAtUtc: '2026-01-02T00:00:00Z',
  updatedAtUtc: null,
};

describe('ClientTableComponent', () => {
  let component: ClientTableComponent;
  let fixture: ComponentFixture<ClientTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientTableComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ClientTableComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('clients', [mockClient, mockClient2]);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render a row for each client', () => {
    const rows = fixture.nativeElement.querySelectorAll(
      'tbody tr',
    ) as NodeListOf<HTMLTableRowElement>;
    expect(rows.length).toBe(2);
  });

  it('should show formatted name as "lastName, firstName"', () => {
    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.textContent).toContain('Bakker, Anna');
  });

  it('should show "—" for null email', () => {
    const rows = fixture.nativeElement.querySelectorAll(
      'tbody tr',
    ) as NodeListOf<HTMLTableRowElement>;
    expect(rows[1].textContent).toContain('—');
  });

  it('should emit edit event on Bewerken click', () => {
    let emitted: ClientResponse | undefined;
    component.edit.subscribe((c) => {
      emitted = c;
    });

    const editButton = fixture.nativeElement.querySelector(
      '[title="Klant bewerken"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('client-1');
  });

  it('should emit delete event on Verwijderen click', () => {
    let emitted: ClientResponse | undefined;
    component.delete.subscribe((c) => {
      emitted = c;
    });

    const deleteButton = fixture.nativeElement.querySelector(
      '[title="Klant verwijderen"]',
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(emitted?.id).toBe('client-1');
  });

  it('should show empty state when clients array is empty', () => {
    fixture.componentRef.setInput('clients', []);
    fixture.detectChanges();

    const emptyCell = fixture.nativeElement.querySelector(
      'td[colspan="4"]',
    ) as HTMLTableCellElement | null;
    expect(emptyCell).toBeTruthy();
    expect(emptyCell?.textContent).toContain('Geen klanten gevonden');
  });
});
