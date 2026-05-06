const formatter = new Intl.DateTimeFormat('nl-NL', {
  month: 'long',
  year: 'numeric',
});

export function formatMonthLabel(date: Date): string {
  const raw = formatter.format(date);
  return raw.charAt(0).toUpperCase() + raw.slice(1);
}
