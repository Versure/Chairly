export function newsletterStatusLabel(status: string): string {
  switch (status) {
    case 'Draft':
      return 'Concept';
    case 'Scheduled':
      return 'Ingepland';
    case 'Sending':
      return 'Wordt verzonden';
    case 'Sent':
      return 'Verzonden';
    case 'Cancelled':
      return 'Geannuleerd';
    default:
      return status;
  }
}
