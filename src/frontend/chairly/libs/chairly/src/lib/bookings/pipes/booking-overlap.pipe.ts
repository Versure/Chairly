import { Pipe, PipeTransform } from '@angular/core';

import { Booking, BookingOverlapPosition } from '../models';

interface TimedBooking {
  id: string;
  startMs: number;
  endMs: number;
}

@Pipe({
  name: 'bookingOverlap',
  standalone: true,
})
export class BookingOverlapPipe implements PipeTransform {
  private cachedBookings: Booking[] = [];
  private cachedPositions = new Map<string, BookingOverlapPosition>();

  transform(bookingId: string, bookings: Booking[]): BookingOverlapPosition {
    if (bookings !== this.cachedBookings) {
      this.cachedBookings = bookings;
      this.cachedPositions = computeAllPositions(bookings);
    }
    return this.cachedPositions.get(bookingId) ?? { columnIndex: 0, totalColumns: 1 };
  }
}

function toTimedBookings(bookings: Booking[]): TimedBooking[] {
  return bookings
    .map((b) => ({
      id: b.id,
      startMs: new Date(b.startTime).getTime(),
      endMs: new Date(b.endTime).getTime(),
    }))
    .sort((a, b) => a.startMs - b.startMs || a.endMs - b.endMs);
}

function buildClusters(sorted: TimedBooking[]): TimedBooking[][] {
  const clusters: TimedBooking[][] = [];
  let currentCluster: TimedBooking[] = [];
  let clusterEnd = 0;

  for (const booking of sorted) {
    if (currentCluster.length > 0 && booking.startMs >= clusterEnd) {
      clusters.push(currentCluster);
      currentCluster = [];
    }
    currentCluster.push(booking);
    clusterEnd = Math.max(clusterEnd, booking.endMs);
  }

  if (currentCluster.length > 0) {
    clusters.push(currentCluster);
  }

  return clusters;
}

function assignColumns(cluster: TimedBooking[]): Map<string, BookingOverlapPosition> {
  const positions = new Map<string, BookingOverlapPosition>();
  const columnEnds: number[] = [];

  for (const booking of cluster) {
    const col = columnEnds.findIndex((end) => end <= booking.startMs);
    const assignedColumn = col >= 0 ? col : columnEnds.length;

    if (assignedColumn === columnEnds.length) {
      columnEnds.push(booking.endMs);
    } else {
      columnEnds[assignedColumn] = booking.endMs;
    }

    positions.set(booking.id, { columnIndex: assignedColumn, totalColumns: 0 });
  }

  const totalColumns = columnEnds.length;
  for (const pos of positions.values()) {
    pos.totalColumns = totalColumns;
  }

  return positions;
}

function computeAllPositions(bookings: Booking[]): Map<string, BookingOverlapPosition> {
  const result = new Map<string, BookingOverlapPosition>();

  if (bookings.length === 0) {
    return result;
  }

  const sorted = toTimedBookings(bookings);
  const clusters = buildClusters(sorted);

  for (const cluster of clusters) {
    const clusterPositions = assignColumns(cluster);
    for (const [id, pos] of clusterPositions) {
      result.set(id, pos);
    }
  }

  return result;
}
