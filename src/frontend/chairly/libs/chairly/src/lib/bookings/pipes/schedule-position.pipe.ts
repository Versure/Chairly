import { Pipe, PipeTransform } from '@angular/core';

import { ScheduleRange } from '../models';

@Pipe({
  name: 'scheduleTop',
  standalone: true,
})
export class ScheduleTopPipe implements PipeTransform {
  transform(startTime: string, range: ScheduleRange): number {
    const date = new Date(startTime);
    const hours = date.getHours();
    const minutes = date.getMinutes();
    const startMinutes = range.startHour * 60;
    const totalMinutes = (range.endHour - range.startHour) * 60;
    const minutesFromStart = hours * 60 + minutes - startMinutes;
    return Math.max(0, Math.min((minutesFromStart / totalMinutes) * 100, 100));
  }
}

@Pipe({
  name: 'scheduleHeight',
  standalone: true,
})
export class ScheduleHeightPipe implements PipeTransform {
  transform(startTime: string, endTime: string, range: ScheduleRange): number {
    const start = new Date(startTime);
    const end = new Date(endTime);
    const durationMinutes = (end.getTime() - start.getTime()) / 60000;
    const totalMinutes = (range.endHour - range.startHour) * 60;
    return Math.max(0, Math.min((durationMinutes / totalMinutes) * 100, 100));
  }
}

@Pipe({
  name: 'timeSlots',
  standalone: true,
})
export class TimeSlotsPipe implements PipeTransform {
  transform(range: ScheduleRange): string[] {
    const slots: string[] = [];
    for (let hour = range.startHour; hour <= range.endHour; hour++) {
      slots.push(`${hour.toString().padStart(2, '0')}:00`);
      if (hour < range.endHour) {
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
  transform(slot: string, range: ScheduleRange): number {
    const [hours, minutes] = slot.split(':').map(Number);
    const startMinutes = range.startHour * 60;
    const totalMinutes = (range.endHour - range.startHour) * 60;
    const minutesFromStart = hours * 60 + minutes - startMinutes;
    return (minutesFromStart / totalMinutes) * 100;
  }
}
