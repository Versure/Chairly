import { type Locator, type Page } from '@playwright/test';

/**
 * Opens the Flatpickr calendar by clicking the input, with fallback to
 * programmatic open. Flatpickr's click-to-open can fail when initialized
 * inside a hidden <dialog> element (display:none at init time).
 */
async function openFlatpickr(page: Page, inputLocator: Locator): Promise<void> {
  await inputLocator.click();

  // Wait briefly for the calendar to open via the native click handler
  const calendar = page.locator('.flatpickr-calendar.open');
  const opened = await calendar.isVisible().catch(() => false);
  if (opened) return;

  // Fallback: find the hidden Flatpickr input (sibling of the alt input) and
  // programmatically open the calendar via Flatpickr's documented open() method.
  // This is necessary when Flatpickr was initialized while the input was inside
  // a hidden <dialog> and the click-to-open binding did not attach correctly.
  // Fallback: programmatically open the calendar if the native click didn't trigger it.
  // This handles the case where Flatpickr was initialized while the input was inside
  // a hidden <dialog> and the click-to-open binding did not attach correctly.
  await inputLocator.evaluate((el: HTMLElement) => {
    const wrapper = el.closest('.flatpickr-wrapper');
    if (!wrapper) return;
    const hiddenInput = wrapper.querySelector<HTMLInputElement>('input[type="hidden"]');
    /* eslint-disable @typescript-eslint/no-explicit-any */
    const fp = (hiddenInput as any)?._flatpickr;
    /* eslint-enable @typescript-eslint/no-explicit-any */
    if (fp) fp.open();
  });

  await calendar.waitFor({ state: 'visible', timeout: 5000 });
}

/**
 * Opens a Flatpickr calendar by clicking the input, selects a day, and confirms.
 * For date and datetime modes.
 */
export async function selectFlatpickrDate(
  page: Page,
  inputLocator: Locator,
  day: number,
): Promise<void> {
  await openFlatpickr(page, inputLocator);
  await page
    .locator('.flatpickr-calendar.open .flatpickr-day:not(.flatpickr-disabled)')
    .filter({ hasText: new RegExp(`^${day}$`) })
    .first()
    .click();
  await page.locator('.flatpickr-calendar.open .flatpickr-confirm').click();
}

/**
 * Opens a Flatpickr calendar, selects a day with specific time, and confirms.
 * For datetime mode.
 */
export async function selectFlatpickrDateTime(
  page: Page,
  inputLocator: Locator,
  day: number,
  hour: string,
  minute: string,
): Promise<void> {
  await openFlatpickr(page, inputLocator);
  await page
    .locator('.flatpickr-calendar.open .flatpickr-day:not(.flatpickr-disabled)')
    .filter({ hasText: new RegExp(`^${day}$`) })
    .first()
    .click();
  await page.locator('.flatpickr-calendar.open .flatpickr-hour').fill(hour);
  await page.locator('.flatpickr-calendar.open .flatpickr-minute').fill(minute);
  await page.locator('.flatpickr-calendar.open .flatpickr-confirm').click();
}

/**
 * Opens a Flatpickr time picker, sets hour and minute, and confirms.
 * For time-only mode.
 */
export async function selectFlatpickrTime(
  page: Page,
  inputLocator: Locator,
  hour: string,
  minute: string,
): Promise<void> {
  await openFlatpickr(page, inputLocator);
  await page.locator('.flatpickr-calendar.open .flatpickr-hour').fill(hour);
  await page.locator('.flatpickr-calendar.open .flatpickr-minute').fill(minute);
  await page.locator('.flatpickr-calendar.open .flatpickr-confirm').click();
}
