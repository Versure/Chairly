/**
 * Parses a .NET TimeSpan string ('HH:MM:SS') to total minutes.
 * e.g. '01:30:00' → 90
 */
export function parseDuration(timeSpan: string): number {
  const parts = timeSpan.split(':');
  const hours = parseInt(parts[0], 10);
  const minutes = parseInt(parts[1], 10);
  return hours * 60 + minutes;
}

/**
 * Converts total minutes to a .NET TimeSpan string ('HH:MM:SS').
 * e.g. 90 → '01:30:00'
 */
export function formatDurationToTimeSpan(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  const hh = String(hours).padStart(2, '0');
  const mm = String(remainingMinutes).padStart(2, '0');
  return `${hh}:${mm}:00`;
}
