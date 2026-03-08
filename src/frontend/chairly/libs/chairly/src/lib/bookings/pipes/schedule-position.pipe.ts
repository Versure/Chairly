import { Pipe, PipeTransform } from '@angular/core';

const SCHEDULE_START_HOUR = 8;
const SCHEDULE_END_HOUR = 20;
const TOTAL_MINUTES = (SCHEDULE_END_HOUR - SCHEDULE_START_HOUR) * 60;

@Pipe({
  name: 'scheduleTop',
  standalone: true,
})
export class ScheduleTopPipe implements PipeTransform {
  transform(startTime: string): number {
    const date = new Date(startTime);
    const minutesFromStart = (date.getHours() - SCHEDULE_START_HOUR) * 60 + date.getMinutes();
    return Math.max(0, Math.min((minutesFromStart / TOTAL_MINUTES) * 100, 100));
  }
}

@Pipe({
  name: 'scheduleHeight',
  standalone: true,
})
export class ScheduleHeightPipe implements PipeTransform {
  transform(startTime: string, endTime: string): number {
    const start = new Date(startTime);
    const end = new Date(endTime);
    const durationMinutes = (end.getTime() - start.getTime()) / 60000;
    return Math.max(0, Math.min((durationMinutes / TOTAL_MINUTES) * 100, 100));
  }
}

@Pipe({
  name: 'timeSlots',
  standalone: true,
})
export class TimeSlotsPipe implements PipeTransform {
  transform(_trigger: boolean): string[] {
    const slots: string[] = [];
    for (let hour = SCHEDULE_START_HOUR; hour <= SCHEDULE_END_HOUR; hour++) {
      slots.push(`${hour.toString().padStart(2, '0')}:00`);
      if (hour < SCHEDULE_END_HOUR) {
        slots.push(`${hour.toString().padStart(2, '0')}:30`);
      }
    }
    return slots;
  }
}

@Pipe({
  name: 'timeSlotTop',
  standalone: true,
})
export class TimeSlotTopPipe implements PipeTransform {
  transform(slot: string): number {
    const [hours, minutes] = slot.split(':').map(Number);
    const minutesFromStart = (hours - SCHEDULE_START_HOUR) * 60 + minutes;
    return (minutesFromStart / TOTAL_MINUTES) * 100;
  }
}
