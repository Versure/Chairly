import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';

import { BookingStatus } from '../../models';

interface ChipViewModel {
  label: string;
  value: BookingStatus | 'All';
  count: number;
  selected: boolean;
}

interface ChipDef {
  label: string;
  value: BookingStatus | 'All';
  isGepland: boolean;
}

const CHIP_DEFS: ChipDef[] = [
  { label: 'Alle', value: 'All', isGepland: false },
  { label: 'Voltooid', value: 'Completed', isGepland: false },
  { label: 'Geannuleerd', value: 'Cancelled', isGepland: false },
  { label: 'No-show', value: 'NoShow', isGepland: false },
  { label: 'Gepland', value: 'Scheduled', isGepland: true },
];

@Component({
  selector: 'chairly-timeline-status-filter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './timeline-status-filter.component.html',
})
export class TimelineStatusFilterComponent {
  readonly value = model.required<BookingStatus | 'All'>();
  readonly counts = input.required<Record<BookingStatus | 'All', number>>();

  protected readonly chipViewModels = computed<ChipViewModel[]>(() => {
    const c = this.counts();
    const selected = this.value();
    return CHIP_DEFS.map((def) => ({
      label: def.label,
      value: def.value,
      count: def.isGepland
        ? (c['Scheduled'] ?? 0) + (c['Confirmed'] ?? 0) + (c['InProgress'] ?? 0)
        : (c[def.value] ?? 0),
      selected: selected === def.value,
    }));
  });

  protected selectChip(chipValue: BookingStatus | 'All'): void {
    this.value.set(chipValue);
  }
}
