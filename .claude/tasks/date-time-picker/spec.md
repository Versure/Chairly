# Date and DateTime Picker

## Overview

Replace all native browser `<input type="date">`, `<input type="datetime-local">`, and `<input type="time">` elements with a custom Flatpickr-based `<chairly-date-picker>` component. The native browser pickers lack a confirm button, forcing users to click outside to dismiss them. This feature introduces a proper date/time picker with a "Bevestigen" button, Dutch locale formatting (`dd-MM-yyyy HH:mm`, 24h), the ability to disable specific dates, and full reactive forms integration. This is a frontend-only feature (no backend changes).

**Supersedes:** `.claude/tasks/date-picker-confirm/spec.md` -- the previous spec proposed wrapping native inputs in a popover. This spec replaces that approach entirely with Flatpickr.

## Domain Context

- **Bounded context:** Shared (frontend-only, UI component)
- **Key entities involved:** none (UX improvement)
- **Ubiquitous language:** no domain terms affected
- **Key files involved:**
  - `libs/shared/src/lib/ui/date-picker/` -- new shared component
  - `libs/shared/src/lib/ui/index.ts` -- barrel export
  - `libs/chairly/src/lib/bookings/feature/booking-list-page/` -- date filter
  - `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.*` -- datetime-local input (note: this file is flat inside `ui/`, which is a pre-existing violation of the subfolder convention; moving it to `ui/booking-form-dialog/` is out of scope for this feature)
  - `libs/chairly/src/lib/billing/feature/invoice-list-page/` -- date range filters
  - `libs/chairly/src/lib/staff/ui/shift-schedule-editor/` -- time inputs
  - `apps/chairly-e2e/src/bookings.spec.ts` -- booking e2e tests
  - `apps/chairly-e2e/src/billing.spec.ts` -- billing e2e tests
  - `apps/chairly-e2e/src/staff.spec.ts` -- staff e2e tests

All paths are relative to `src/frontend/chairly/`.

## Frontend Tasks

### F1 — Install Flatpickr and configure styles

Install the Flatpickr npm package and its TypeScript types. Configure the base Flatpickr CSS with custom overrides to match the Chairly Tailwind design system, including dark mode support.

**Steps:**

1. Install packages:
   ```bash
   cd src/frontend/chairly && npm install flatpickr
   ```
   Flatpickr ships its own types, so no separate `@types/flatpickr` is needed.

2. Create a plain CSS entry file at `libs/shared/src/lib/ui/date-picker/date-picker.css` that imports the Flatpickr base theme:
   ```css
   @import 'flatpickr/dist/flatpickr.css';
   ```
   This file is listed in the component's `styleUrls` array. Per CLAUDE.md, CSS library imports must never go inside `.scss` files -- always use a separate plain `.css` entry file.

3. Create a SCSS override file at `libs/shared/src/lib/ui/date-picker/date-picker.component.scss` for custom Chairly overrides only (no `@import` of CSS libraries):
   - Override Flatpickr's default colors to use Chairly design tokens:
     - Calendar background: `bg-white` / `dark:bg-slate-800`
     - Selected day: `bg-primary-600` / `dark:bg-primary-500`
     - Hover day: `bg-primary-50` / `dark:bg-slate-700`
     - Today highlight: `border-primary-500`
     - Text colors: `text-gray-900` / `dark:text-white`
     - Arrow navigation: `text-gray-600` / `dark:text-gray-300`
     - Disabled days: `text-gray-300` / `dark:text-gray-600`
     - Month/year dropdowns: match input styling
     - Confirm button (from confirmDate plugin): `bg-primary-600 text-white hover:bg-primary-700`
   - Use `[data-theme="dark"]` selector for dark mode overrides (matching `ThemeService` convention).
   - Set `encapsulation: ViewEncapsulation.None` on the component so Flatpickr's absolutely-positioned calendar (appended to body or inline) picks up the styles.

4. The component's `styleUrls` array should list both files:
   ```typescript
   styleUrls: ['./date-picker.css', './date-picker.component.scss']
   ```

**Test cases:**
- Visual: Flatpickr calendar renders with Chairly colors in both light and dark mode.

---

### F2 — Create shared `<chairly-date-picker>` component

Create a reusable Flatpickr-based date/time picker in `libs/shared/src/lib/ui/date-picker/`.

**Files to create:**
- `date-picker.component.ts`
- `date-picker.component.html`
- `date-picker.css` (Flatpickr base CSS import -- see F1)
- `date-picker.component.scss` (custom Chairly overrides -- see F1)

**Component API:**

```typescript
// Selector: chairly-date-picker
// ChangeDetectionStrategy.OnPush, standalone
// ViewEncapsulation.None (for Flatpickr styles)
// Implements ControlValueAccessor

mode = input<'date' | 'datetime' | 'time'>('date');
inputId = input<string>('');
placeholder = input<string>('');
minDate = input<string | Date | undefined>(undefined);
maxDate = input<string | Date | undefined>(undefined);
disabledDates = input<Array<string | Date | { from: string | Date; to: string | Date }>>([]);
```

**Implements `ControlValueAccessor`** following the same pattern as `SearchableDropdownComponent`:
- `NG_VALUE_ACCESSOR` provider with `forwardRef`
- `noop` function for initial `onChange`/`onTouched`
- Works with `formControlName`, `[(ngModel)]`, and `[ngModel]` + `(ngModelChange)`

**Value format (ISO strings for .NET compatibility):**
- `mode="date"`: emits `"2026-03-12"` (ISO date string)
- `mode="datetime"`: emits `"2026-03-12T14:30:00"` (ISO datetime without timezone)
- `mode="time"`: emits `"14:30"` (HH:mm 24h format)

**Flatpickr configuration:**

```typescript
// Pseudocode for Flatpickr instance creation
import flatpickr from 'flatpickr';
import { Dutch } from 'flatpickr/dist/l10n/nl';
import confirmDatePlugin from 'flatpickr/dist/plugins/confirmDate/confirmDate';

const instance = flatpickr(inputElement, {
  locale: Dutch,
  enableTime: mode === 'datetime' || mode === 'time',
  noCalendar: mode === 'time',
  dateFormat: this.getDateFormat(), // see below
  time_24hr: true,
  minDate: minDate(),
  maxDate: maxDate(),
  disable: disabledDates(),
  plugins: [
    confirmDatePlugin({
      confirmText: 'Bevestigen',
      showAlways: true,
      theme: 'light', // overridden by our custom CSS
    }),
  ],
  onChange: (selectedDates, dateStr) => {
    // Do NOT commit yet -- wait for confirm
  },
  onClose: (selectedDates, dateStr) => {
    this.commitValue(dateStr);
  },
});
```

**Date format mapping:**
- `mode="date"`: `dateFormat: "d-m-Y"` (display), altInput with `altFormat: "d-m-Y"` and `altInput: true` for display, underlying value as `"Y-m-d"` for ISO
- `mode="datetime"`: `dateFormat: "d-m-Y H:i"` (display), underlying ISO value
- `mode="time"`: `dateFormat: "H:i"`

**Important implementation detail -- altInput pattern:**
Use Flatpickr's `altInput: true` with `altFormat` for Dutch display format and `dateFormat` for the underlying ISO value:
```typescript
{
  altInput: true,
  altFormat: 'd-m-Y',     // What the user sees: 26-03-2026
  dateFormat: 'Y-m-d',    // What the value is: 2026-03-26
  // For datetime:
  // altFormat: 'd-m-Y H:i', dateFormat: 'Y-m-d\\TH:i:S'
  // For time:
  // altFormat: 'H:i', dateFormat: 'H:i'
}
```

**Lifecycle:**
- Create Flatpickr instance in `ngAfterViewInit` using `viewChild` reference to the native input.
- Destroy Flatpickr instance via `DestroyRef`: inject `DestroyRef` and register cleanup with `destroyRef.onDestroy(() => instance.destroy())`. Do NOT use `ngOnDestroy`.
- React to input changes (`minDate`, `maxDate`, `disabledDates`) via `effect()` to call `instance.set()` for dynamic updates.

**Close/confirm behaviour:**
- The confirmDate plugin adds a "Bevestigen" button. Clicking it closes the picker and triggers `onClose`.
- `onClose` callback commits the value by calling `onChange(isoValue)` and `onTouched()`.
- The user can also press Escape to close without confirming (Flatpickr default behaviour).

**Dark mode:**
- The component's SCSS handles dark mode via `[data-theme="dark"]` selectors.
- The trigger input uses standard Tailwind classes: `rounded-md border border-gray-300 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-700 dark:text-white`.

**Template:**

```html
<input
  #pickerInput
  [id]="inputId()"
  [placeholder]="placeholder()"
  type="text"
  readonly
  class="w-full cursor-pointer rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
         focus:outline-none focus:ring-1 focus:ring-primary-500
         dark:border-slate-600 dark:bg-slate-700 dark:text-white" />
```

The input is `readonly` because Flatpickr handles all input interaction. The `type="text"` is intentional -- Flatpickr attaches to text inputs and provides its own calendar/time UI.

**Export** from `libs/shared/src/lib/ui/index.ts`:
```typescript
export { DatePickerComponent } from './date-picker/date-picker.component';
```

**Implementation notes:**
- Follow the `SearchableDropdownComponent` pattern for `ControlValueAccessor` registration.
- Use Angular signals (`signal`, `computed`, `input`) consistently -- no decorators.
- Use `templateUrl:` with a separate `.html` file.
- Use `viewChild` to get a reference to the native input for Flatpickr initialization.
- Use `DestroyRef` (injected via `inject(DestroyRef)`) for cleanup: `destroyRef.onDestroy(() => this.flatpickrInstance?.destroy())`.

---

### F3 — Unit tests for DatePickerComponent

**File to create:** `libs/shared/src/lib/ui/date-picker/date-picker.component.spec.ts`

Write unit tests (Vitest) covering:

1. **Renders with placeholder** -- when no value is set, the input displays the placeholder text.
2. **Displays formatted date value** -- set a date value (`"2026-03-12"`) via `writeValue`, verify the altInput shows `"12-03-2026"` (Dutch dd-MM-yyyy format).
3. **Displays formatted datetime value** -- set `mode="datetime"` and value `"2026-03-12T14:30:00"`, verify display shows `"12-03-2026 14:30"`.
4. **Displays formatted time value** -- set `mode="time"` and value `"14:30"`, verify display shows `"14:30"`.
5. **Emits ISO date string** -- select a date in the picker, confirm, verify the emitted value is `"2026-03-12"` format.
6. **Emits ISO datetime string** -- select a datetime, confirm, verify the emitted value is `"2026-03-12T14:30:00"` format.
7. **Emits time string** -- select a time, confirm, verify the emitted value is `"14:30"` format.
8. **Confirm button text is Dutch** -- open the picker, verify the confirm button text is "Bevestigen".
9. **Respects minDate** -- set `minDate` to today, verify dates before today are disabled/not selectable.
10. **Respects maxDate** -- set `maxDate` to a specific date, verify dates after it are disabled.
11. **Respects disabledDates** -- pass an array of disabled dates, verify those dates are not selectable.
12. **Works with reactive forms** -- wrap in a host component with `formControlName`, verify value propagation.
13. **Works with ngModel** -- use `[(ngModel)]` in a host component, verify two-way binding.
14. **Cleanup on destroy** -- verify Flatpickr instance is destroyed when the component is destroyed via `DestroyRef` (no memory leaks).

---

### F4 — Update booking list page (date filter)

**Files:**
- `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.html`
- `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.ts`

Replace the native `<input type="date">` for the date filter with `<chairly-date-picker>`.

**Current code (HTML):**
```html
<input
  id="filter-date"
  type="date"
  class="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
  [value]="filterDate()"
  (input)="onDateChange($event)" />
```

**Replace with:**
```html
<chairly-date-picker
  mode="date"
  inputId="filter-date"
  [ngModel]="filterDate()"
  (ngModelChange)="onDateFilterChange($event)" />
```

**TypeScript changes:**
- Import `DatePickerComponent` from `@org/shared-lib` and `FormsModule` from `@angular/forms`.
- Add both to the component's `imports` array.
- Replace the `onDateChange(event: Event)` method with `onDateFilterChange(value: string)`:
  ```typescript
  protected onDateFilterChange(value: string): void {
    this.filterDate.set(value);
  }
  ```

---

### F5 — Update booking form dialog (datetime input)

**Files:**
- `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.html`
- `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.ts`

Note: `booking-form-dialog.component.*` is currently flat inside `ui/` rather than in its own `ui/booking-form-dialog/` subfolder. This is a pre-existing convention violation. Moving it is out of scope for this feature; modify the files at their current location.

Replace the native `<input type="datetime-local">` with `<chairly-date-picker>`.

**Current code (HTML):**
```html
<input
  id="bfd-startTime"
  formControlName="startTime"
  type="datetime-local"
  class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-700 dark:text-white" />
```

**Replace with:**
```html
<chairly-date-picker
  mode="datetime"
  inputId="bfd-startTime"
  formControlName="startTime"
  [minDate]="today" />
```

Where `today` is a property returning the current date string to prevent booking in the past:
```typescript
protected readonly today = new Date().toISOString().split('T')[0];
```

**TypeScript changes:**
- Import `DatePickerComponent` from `@org/shared-lib`.
- Add to the component's `imports` array.
- Add the `today` property for `minDate`.
- No other changes needed since the component implements `ControlValueAccessor` and works with `formControlName`.

**Note:** The form control value format changes from `"2026-03-12T14:30"` (datetime-local) to `"2026-03-12T14:30:00"` (with seconds). Verify that the backend accepts both formats or adjust the Flatpickr `dateFormat` to omit seconds if needed.

---

### F6 — Update invoice list page (date range filters)

**Files:**
- `libs/chairly/src/lib/billing/feature/invoice-list-page/invoice-list-page.component.html`
- `libs/chairly/src/lib/billing/feature/invoice-list-page/invoice-list-page.component.ts`

Replace both native `<input type="date">` elements (Datum van, Datum tot) with `<chairly-date-picker>`.

**Current code -- Datum van:**
```html
<input
  id="filter-from-date"
  type="date"
  class="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
  [ngModel]="filterFromDate()"
  (ngModelChange)="filterFromDate.set($event)" />
```

**Replace with:**
```html
<chairly-date-picker
  mode="date"
  inputId="filter-from-date"
  placeholder="Datum van"
  [ngModel]="filterFromDate()"
  (ngModelChange)="filterFromDate.set($event)" />
```

**Current code -- Datum tot:**
```html
<input
  id="filter-to-date"
  type="date"
  class="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
  [ngModel]="filterToDate()"
  (ngModelChange)="filterToDate.set($event)" />
```

**Replace with:**
```html
<chairly-date-picker
  mode="date"
  inputId="filter-to-date"
  placeholder="Datum tot"
  [ngModel]="filterToDate()"
  (ngModelChange)="filterToDate.set($event)" />
```

**TypeScript changes:**
- Import `DatePickerComponent` from `@org/shared-lib`.
- Add to the component's `imports` array.

---

### F7 — Update shift schedule editor (time inputs)

**Files:**
- `libs/chairly/src/lib/staff/ui/shift-schedule-editor/shift-schedule-editor.component.html`
- `libs/chairly/src/lib/staff/ui/shift-schedule-editor/shift-schedule-editor.component.ts`

Replace both native `<input type="time">` elements per shift block with `<chairly-date-picker>`.

**Current code -- start time:**
```html
<input
  type="time"
  [value]="block.startTime"
  (change)="updateStartTime(row.key, i, $event)"
  class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-sm" />
```

**Replace with:**
```html
<chairly-date-picker
  mode="time"
  [ngModel]="block.startTime"
  (ngModelChange)="onStartTimeChange(row.key, i, $event)" />
```

**Current code -- end time:**
```html
<input
  type="time"
  [value]="block.endTime"
  (change)="updateEndTime(row.key, i, $event)"
  class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-sm" />
```

**Replace with:**
```html
<chairly-date-picker
  mode="time"
  [ngModel]="block.endTime"
  (ngModelChange)="onEndTimeChange(row.key, i, $event)" />
```

**TypeScript changes:**
- Import `DatePickerComponent` from `@org/shared-lib` and `FormsModule` from `@angular/forms`.
- Add both to the component's `imports` array.
- Replace `updateStartTime(dayKey, blockIndex, event: Event)` with `onStartTimeChange(dayKey: DayOfWeek, blockIndex: number, value: string)`:
  ```typescript
  protected onStartTimeChange(dayKey: DayOfWeek, blockIndex: number, value: string): void {
    this.updateBlockTime(dayKey, blockIndex, 'startTime', value);
  }
  ```
- Replace `updateEndTime(dayKey, blockIndex, event: Event)` with `onEndTimeChange(dayKey: DayOfWeek, blockIndex: number, value: string)`:
  ```typescript
  protected onEndTimeChange(dayKey: DayOfWeek, blockIndex: number, value: string): void {
    this.updateBlockTime(dayKey, blockIndex, 'endTime', value);
  }
  ```
- The private `updateBlockTime` method remains unchanged.

---

### F8 — Update existing e2e tests

Existing Playwright e2e tests that interact with date/time inputs need to be updated because Flatpickr replaces native inputs with a custom calendar UI.

**New e2e interaction pattern for Flatpickr inputs:**

All e2e tests must interact with the Flatpickr UI the same way a real user would. Do NOT access undocumented private properties like `el._flatpickr`. Instead, use Playwright locators targeting Flatpickr's documented CSS classes.

For date inputs:
1. Click the Flatpickr input (identified by `id` or label) to open the calendar.
2. Navigate to the correct month/year if needed using `.flatpickr-prev-month` / `.flatpickr-next-month`.
3. Click the desired day cell: `.flatpickr-day:not(.flatpickr-disabled)` with matching text.
4. Click the "Bevestigen" button (`.flatpickr-confirm`) to confirm.

For datetime inputs:
1. Click the Flatpickr input to open the calendar.
2. Click the desired day cell.
3. Set the time using the Flatpickr time inputs: `.flatpickr-hour` and `.flatpickr-minute` (fill or use arrow keys).
4. Click the "Bevestigen" button to confirm.

For time-only inputs:
1. Click the Flatpickr input to open the time picker.
2. Set the hour and minute values using `.flatpickr-hour` and `.flatpickr-minute`.
3. Click the "Bevestigen" button to confirm.

**Helper function (recommended):**
Create a Playwright helper in `apps/chairly-e2e/src/helpers/flatpickr.helper.ts` for interacting with Flatpickr through its UI:

```typescript
import { type Locator, type Page } from '@playwright/test';

/**
 * Opens a Flatpickr calendar by clicking the input, selects a day, and confirms.
 * For date and datetime modes.
 */
export async function selectFlatpickrDate(
  page: Page,
  inputLocator: Locator,
  day: number
): Promise<void> {
  await inputLocator.click();
  await page.locator('.flatpickr-calendar.open').waitFor({ state: 'visible' });
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
  minute: string
): Promise<void> {
  await inputLocator.click();
  await page.locator('.flatpickr-calendar.open').waitFor({ state: 'visible' });
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
  minute: string
): Promise<void> {
  await inputLocator.click();
  await page.locator('.flatpickr-calendar.open').waitFor({ state: 'visible' });
  await page.locator('.flatpickr-calendar.open .flatpickr-hour').fill(hour);
  await page.locator('.flatpickr-calendar.open .flatpickr-minute').fill(minute);
  await page.locator('.flatpickr-calendar.open .flatpickr-confirm').click();
}
```

**Files to review and update:**

- `apps/chairly-e2e/src/bookings.spec.ts` -- The test that creates a new booking fills a `datetime-local` input with `dialog.getByLabel('Datum & tijd').fill('2026-03-10T11:00')`. This must be updated to use `selectFlatpickrDateTime` to click the input, select day 10, set hour to `11` and minute to `00`, and click "Bevestigen".

- `apps/chairly-e2e/src/billing.spec.ts` -- Review if any test fills date filter inputs. Currently the billing e2e tests do not directly fill the date filter inputs, so changes may be minimal or none.

- `apps/chairly-e2e/src/staff.spec.ts` -- Review if any test fills time inputs. Currently the staff e2e tests do not directly fill time inputs in the shift schedule, so changes may be minimal or none.

For each affected test, ensure:
- The Flatpickr input click opens the calendar/time picker.
- Day cells / time inputs are interacted with using documented CSS class selectors.
- The "Bevestigen" button is clicked to confirm the value.
- The test waits for the calendar to close before continuing.

---

## Acceptance Criteria

- [ ] Flatpickr is installed as an npm dependency
- [ ] A shared `<chairly-date-picker>` component exists in `libs/shared/src/lib/ui/date-picker/`
- [ ] Component implements `ControlValueAccessor` and works with `formControlName`, `[(ngModel)]`, and `[ngModel]`+`(ngModelChange)`
- [ ] Component supports `mode="date"`, `mode="datetime"`, and `mode="time"`
- [ ] Flatpickr calendar renders with Chairly design tokens (primary colors, rounded corners)
- [ ] Dark mode styling works correctly (calendar, inputs, confirm button)
- [ ] Dates display in Dutch format: `dd-MM-yyyy` for date, `dd-MM-yyyy HH:mm` for datetime, `HH:mm` for time
- [ ] Time is displayed in 24-hour format
- [ ] The "Bevestigen" confirm button is visible and commits the selection
- [ ] `minDate`, `maxDate`, and `disabledDates` inputs work correctly
- [ ] Values emitted are ISO strings compatible with the .NET backend
- [ ] Dutch locale is active (month names, day names in Dutch)
- [ ] All 6 native date/time inputs across the app are replaced (1 booking list, 1 booking form, 2 invoice list, 2 shift schedule editor)
- [ ] `DatePickerComponent` is exported from `libs/shared/src/lib/ui/index.ts`
- [ ] Flatpickr base CSS is imported via a plain `.css` file, not inside `.scss`
- [ ] Flatpickr instance cleanup uses `DestroyRef.onDestroy()`, not `ngOnDestroy`
- [ ] Unit tests for `DatePickerComponent` pass (14 test cases)
- [ ] All frontend quality checks pass (`npx nx affected -t lint`, `npx nx format:check`, `npx nx affected -t test`, `npx nx affected -t build`)
- [ ] Existing e2e tests are updated and pass
- [ ] E2e tests interact with Flatpickr via documented CSS classes, not private APIs

## Out of Scope

- Date range picker as a single component (invoice filters remain two separate date-picker instances)
- Backend changes (this is a frontend-only feature)
- Custom Flatpickr plugins beyond confirmDate
- Inline/always-open calendar mode
- Mobile-specific touch optimizations beyond what Flatpickr provides natively
- Moving `booking-form-dialog.component.*` into its own `ui/booking-form-dialog/` subfolder (pre-existing convention violation)
