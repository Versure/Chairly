import { ChangeDetectionStrategy, Component, computed, model, ModelSignal } from '@angular/core';

import { DayOfWeek, ShiftBlock, WeeklySchedule } from '../../models';

interface DayRow {
  key: DayOfWeek;
  label: string;
  isActive: boolean;
  blocks: ShiftBlock[];
}

@Component({
  selector: 'chairly-shift-schedule-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shift-schedule-editor.component.html',
})
export class ShiftScheduleEditorComponent {
  readonly schedule: ModelSignal<WeeklySchedule> = model<WeeklySchedule>({});

  private readonly dayDefinitions: ReadonlyArray<{ key: DayOfWeek; label: string }> = [
    { key: 'monday', label: 'Maandag' },
    { key: 'tuesday', label: 'Dinsdag' },
    { key: 'wednesday', label: 'Woensdag' },
    { key: 'thursday', label: 'Donderdag' },
    { key: 'friday', label: 'Vrijdag' },
    { key: 'saturday', label: 'Zaterdag' },
    { key: 'sunday', label: 'Zondag' },
  ];

  protected readonly dayRows = computed<DayRow[]>(() => {
    const schedule = this.schedule();
    return this.dayDefinitions.map((day) => ({
      key: day.key,
      label: day.label,
      isActive: (schedule[day.key]?.length ?? 0) > 0,
      blocks: schedule[day.key] ?? [],
    }));
  });

  protected toggleDay(dayKey: DayOfWeek, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const current = { ...this.schedule() };
    if (checked) {
      current[dayKey] = [{ startTime: '09:00', endTime: '17:00' }];
    } else {
      delete current[dayKey];
    }
    this.schedule.set(current);
  }

  protected addBlock(dayKey: DayOfWeek): void {
    const current = { ...this.schedule() };
    const existing = current[dayKey] ?? [];
    current[dayKey] = [...existing, { startTime: '09:00', endTime: '17:00' }];
    this.schedule.set(current);
  }

  protected removeBlock(dayKey: DayOfWeek, blockIndex: number): void {
    const current = { ...this.schedule() };
    const existing = [...(current[dayKey] ?? [])];
    existing.splice(blockIndex, 1);
    if (existing.length === 0) {
      delete current[dayKey];
    } else {
      current[dayKey] = existing;
    }
    this.schedule.set(current);
  }

  protected updateStartTime(dayKey: DayOfWeek, blockIndex: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.updateBlockTime(dayKey, blockIndex, 'startTime', value);
  }

  protected updateEndTime(dayKey: DayOfWeek, blockIndex: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.updateBlockTime(dayKey, blockIndex, 'endTime', value);
  }

  private updateBlockTime(
    dayKey: DayOfWeek,
    blockIndex: number,
    field: 'startTime' | 'endTime',
    value: string,
  ): void {
    const current = { ...this.schedule() };
    const blocks = [...(current[dayKey] ?? [])];
    blocks[blockIndex] = { ...blocks[blockIndex], [field]: value };
    current[dayKey] = blocks;
    this.schedule.set(current);
  }
}
