import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WeeklySchedule } from '../../models';
import { ShiftScheduleEditorComponent } from './shift-schedule-editor.component';

describe('ShiftScheduleEditorComponent', () => {
  let component: ShiftScheduleEditorComponent;
  let fixture: ComponentFixture<ShiftScheduleEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShiftScheduleEditorComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ShiftScheduleEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render 7 day rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('div.border') as NodeListOf<HTMLDivElement>;
    expect(rows.length).toBe(7);
  });

  it('should add a shift block and increase block count for that day', () => {
    const initialSchedule: WeeklySchedule = {
      monday: [{ startTime: '09:00', endTime: '17:00' }],
    };
    fixture.componentRef.setInput('schedule', initialSchedule);
    fixture.detectChanges();

    // Initially 1 block = 2 date-picker components
    let timeInputs = fixture.nativeElement.querySelectorAll('chairly-date-picker');
    expect(timeInputs.length).toBe(2);

    // Click "+ Dienst toevoegen"
    const addButton = Array.from(
      fixture.nativeElement.querySelectorAll('button[type="button"]'),
    ).find(
      (b) => (b as HTMLButtonElement).textContent?.trim() === '+ Dienst toevoegen',
    ) as HTMLButtonElement;
    expect(addButton).toBeTruthy();
    addButton.click();
    fixture.detectChanges();

    // Now 2 blocks = 4 date-picker components
    timeInputs = fixture.nativeElement.querySelectorAll('chairly-date-picker');
    expect(timeInputs.length).toBe(4);
  });

  it('should remove a block and decrease block count for that day', () => {
    const initialSchedule: WeeklySchedule = {
      monday: [
        { startTime: '09:00', endTime: '12:00' },
        { startTime: '13:00', endTime: '17:00' },
      ],
    };
    fixture.componentRef.setInput('schedule', initialSchedule);
    fixture.detectChanges();

    // Initially 2 blocks = 4 date-picker components
    let timeInputs = fixture.nativeElement.querySelectorAll('chairly-date-picker');
    expect(timeInputs.length).toBe(4);

    // Click first remove button (×)
    const removeButton = fixture.nativeElement.querySelector(
      'button[aria-label="Dienst 1 verwijderen"]',
    ) as HTMLButtonElement;
    expect(removeButton).toBeTruthy();
    removeButton.click();
    fixture.detectChanges();

    // Now 1 block = 2 date-picker components
    timeInputs = fixture.nativeElement.querySelectorAll('chairly-date-picker');
    expect(timeInputs.length).toBe(2);
  });

  it('should emit updated schedule when toggling a day on', () => {
    let emitted: WeeklySchedule | undefined;
    component.schedule.subscribe((s) => {
      emitted = s;
    });

    fixture.componentRef.setInput('schedule', {});
    fixture.detectChanges();

    // Check Monday's checkbox (first row)
    const checkboxes = fixture.nativeElement.querySelectorAll(
      'input[type="checkbox"]',
    ) as NodeListOf<HTMLInputElement>;
    const mondayCheckbox = checkboxes[0];
    mondayCheckbox.checked = true;
    mondayCheckbox.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(emitted).toBeDefined();
    expect(emitted?.['monday']).toBeDefined();
    expect(emitted?.['monday']?.length).toBe(1);
  });

  it('should emit updated schedule when adding a block', () => {
    const initialSchedule: WeeklySchedule = {
      monday: [{ startTime: '09:00', endTime: '17:00' }],
    };
    let emitted: WeeklySchedule | undefined;
    component.schedule.subscribe((s) => {
      emitted = s;
    });

    fixture.componentRef.setInput('schedule', initialSchedule);
    fixture.detectChanges();

    const addButton = Array.from(
      fixture.nativeElement.querySelectorAll('button[type="button"]'),
    ).find(
      (b) => (b as HTMLButtonElement).textContent?.trim() === '+ Dienst toevoegen',
    ) as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    expect(emitted).toBeDefined();
    expect(emitted?.['monday']?.length).toBe(2);
  });

  it('should show validation error when endTime is before startTime', () => {
    const invalidSchedule: WeeklySchedule = {
      monday: [{ startTime: '17:00', endTime: '09:00' }],
    };
    fixture.componentRef.setInput('schedule', invalidSchedule);
    fixture.detectChanges();

    const errorMessage = fixture.nativeElement.querySelector(
      'p.text-red-600',
    ) as HTMLParagraphElement;
    expect(errorMessage).toBeTruthy();
    expect(errorMessage.textContent?.trim()).toBe('Eindtijd moet na begintijd liggen');
  });

  it('should not show validation error when endTime is after startTime', () => {
    const validSchedule: WeeklySchedule = {
      monday: [{ startTime: '09:00', endTime: '17:00' }],
    };
    fixture.componentRef.setInput('schedule', validSchedule);
    fixture.detectChanges();

    const errorMessage = fixture.nativeElement.querySelector(
      'p.text-red-600',
    ) as HTMLParagraphElement | null;
    expect(errorMessage).toBeNull();
  });
});
