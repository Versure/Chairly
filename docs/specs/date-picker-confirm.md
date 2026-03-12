# Date and DateTime Picker Confirm Button

## Overview

Native `<input type="date">`, `<input type="datetime-local">`, and `<input type="time">` elements have no explicit way to close the browser's picker popup. Users must click outside the element to dismiss it, which is unintuitive. This spec introduces a shared wrapper component that opens the native input inside a dropdown popover with a "Bevestigen" (Confirm) button, giving users an explicit action to close the picker. Fixes GitHub issue #57.

---

## Domain Context

- **Bounded context:** Shared (frontend-only, UI component)
- **Key entities involved:** none (UX improvement)
- **Ubiquitous language:** no domain terms affected
- **Key files involved:**
  - `libs/shared/src/lib/ui/date-input/` — new shared component (to be created)
  - `libs/shared/src/lib/ui/index.ts` — barrel export
  - `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.html` — date filter
  - `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.html` — datetime-local input
  - `libs/chairly/src/lib/billing/feature/invoice-list-page/invoice-list-page.component.html` — date range filters
  - `libs/chairly/src/lib/staff/ui/shift-schedule-editor/shift-schedule-editor.component.html` — time inputs

All paths are relative to `src/frontend/chairly/`.

---

## Frontend Tasks

### F1 — Create shared `<chairly-date-input>` component

Create a reusable date/time input wrapper that opens a dropdown popover containing the native browser input plus a confirm button.

**Folder:** `libs/shared/src/lib/ui/date-input/`

**Files:**
- `date-input.component.ts`
- `date-input.component.html`
- `date-input.component.scss` (if needed for popover positioning)

**Component API:**

```typescript
// Selector: chairly-date-input
// ChangeDetectionStrategy.OnPush, standalone
// Implements ControlValueAccessor

type = input<'date' | 'datetime-local' | 'time'>('date');
inputId = input<string>('');
placeholder = input<string>('');
```

Implements `ControlValueAccessor` (same pattern as `SearchableDropdownComponent`) so it works with:
- `formControlName="..."` (reactive forms)
- `[(ngModel)]="..."` (template-driven)
- `[ngModel]` + `(ngModelChange)` (one-way with event)

**Behaviour:**

1. **Trigger:** A styled button/input-like element that displays the current value (formatted for readability) or the placeholder when empty. Must visually match the existing input styling: `rounded-md border border-gray-300 px-3 py-2 text-sm` + dark mode classes. Include a small calendar/clock icon on the right side to indicate it's a picker (use an inline SVG or Unicode character).

2. **Popover:** On click of the trigger, open a dropdown panel positioned below the trigger (absolutely positioned, `z-10`). The popover contains:
   - The native `<input>` element with the appropriate `[type]` (`date`, `datetime-local`, or `time`). The input should be auto-focused when the popover opens so the browser picker activates immediately.
   - A footer row with two buttons:
     - "Annuleren" (Cancel) — secondary style, closes the popover and restores the previous value.
     - "Bevestigen" (Confirm) — primary style, commits the selected value and closes the popover.

3. **Close behaviour:**
   - Clicking "Bevestigen" commits the value (calls `onChange` + `onTouched`) and closes the popover.
   - Clicking "Annuleren" restores the previously committed value and closes the popover.
   - Pressing `Escape` acts like "Annuleren" (close without committing).
   - Clicking outside the component acts like "Annuleren" (close without committing). Use `document:click` + `elementRef.contains()` (same pattern as `SearchableDropdownComponent`).

4. **Value formatting for the trigger display:**
   - `type="date"`: format as `DD-MM-YYYY` (Dutch convention). Use `Intl.DateTimeFormat('nl-NL')` or manual formatting.
   - `type="datetime-local"`: format as `DD-MM-YYYY HH:mm`.
   - `type="time"`: display as-is (`HH:mm`).
   - When no value is set, show the placeholder text in a muted colour.

5. **Dark mode:** The popover panel must use `bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600` styling. Buttons follow existing `btn-primary` / secondary button patterns in the codebase.

6. **Accessibility:**
   - The trigger should have `role="combobox"` or similar, with `aria-expanded` reflecting the open state.
   - The popover should trap focus between the input and the buttons.
   - `aria-label` on the trigger should include the input purpose (passed via the `inputId` or a separate label input).

**Template sketch:**

```html
<div class="relative">
  <!-- Trigger -->
  <button
    type="button"
    [id]="inputId()"
    (click)="toggle()"
    (keydown.escape)="cancel()"
    [attr.aria-expanded]="isOpen()"
    class="flex w-full items-center justify-between rounded-md border border-gray-300 bg-white
           px-3 py-2 text-left text-sm text-gray-900
           focus:outline-none focus:ring-1 focus:ring-primary-500
           dark:border-slate-600 dark:bg-slate-700 dark:text-white">
    <span [class.text-gray-400]="!value()" [class.dark:text-slate-400]="!value()">
      {{ displayValue() || placeholder() }}
    </span>
    <!-- Calendar/clock icon SVG -->
  </button>

  <!-- Popover -->
  @if (isOpen()) {
    <div class="absolute z-10 mt-1 w-full rounded-lg border border-gray-200 bg-white
                p-3 shadow-lg dark:border-slate-600 dark:bg-slate-800">
      <input
        #nativeInput
        [type]="type()"
        [value]="tempValue()"
        (input)="onNativeInput($event)"
        class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm
               focus:outline-none focus:ring-1 focus:ring-primary-500
               dark:border-slate-600 dark:bg-slate-700 dark:text-white" />
      <div class="mt-3 flex justify-end gap-2">
        <button
          type="button"
          (click)="cancel()"
          class="rounded-md border border-gray-300 px-3 py-1.5 text-sm text-gray-700
                 hover:bg-gray-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700">
          Annuleren
        </button>
        <button
          type="button"
          (click)="confirm()"
          class="rounded-md bg-primary-600 px-3 py-1.5 text-sm text-white
                 hover:bg-primary-700 dark:bg-primary-500 dark:hover:bg-primary-600">
          Bevestigen
        </button>
      </div>
    </div>
  }
</div>
```

**Export** from `libs/shared/src/lib/ui/index.ts`:
```typescript
export { DateInputComponent } from './date-input/date-input.component';
```

---

### F2 — Update booking list page (date filter)

**File:** `libs/chairly/src/lib/bookings/feature/booking-list-page/booking-list-page.component.html`

Replace the native `<input type="date">` for the date filter with `<chairly-date-input>`.

**Before:**
```html
<input
  id="filter-date"
  type="date"
  class="rounded-md border border-gray-300 px-3 py-2 text-sm ..."
  [value]="filterDate()"
  (input)="onDateChange($event)" />
```

**After:**
```html
<chairly-date-input
  type="date"
  inputId="filter-date"
  [ngModel]="filterDate()"
  (ngModelChange)="onDateFilterChange($event)" />
```

Update the component `.ts` file:
- Import `DateInputComponent` from `@org/shared-lib` and `FormsModule` from `@angular/forms`.
- Add both to the component's `imports`.
- Replace `onDateChange($event: Event)` with `onDateFilterChange(value: string)` that directly receives the string value (no need to extract from `event.target`).

---

### F3 — Update booking form dialog (datetime-local input)

**File:** `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.html`

Replace the native `<input type="datetime-local">` with `<chairly-date-input>`.

**Before:**
```html
<input
  id="bfd-startTime"
  formControlName="startTime"
  type="datetime-local"
  class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm ..." />
```

**After:**
```html
<chairly-date-input
  type="datetime-local"
  inputId="bfd-startTime"
  formControlName="startTime" />
```

Update the component `.ts` file:
- Import `DateInputComponent` from `@org/shared-lib`.
- Add to the component's `imports`.
- No other changes needed since the component implements `ControlValueAccessor`.

---

### F4 — Update invoice list page (date range filters)

**File:** `libs/chairly/src/lib/billing/feature/invoice-list-page/invoice-list-page.component.html`

Replace both native `<input type="date">` elements with `<chairly-date-input>`.

**Before (Datum van):**
```html
<input
  id="filter-from-date"
  type="date"
  class="rounded-md border border-gray-300 px-3 py-2 text-sm ..."
  [ngModel]="filterFromDate()"
  (ngModelChange)="filterFromDate.set($event)" />
```

**After:**
```html
<chairly-date-input
  type="date"
  inputId="filter-from-date"
  placeholder="Datum van"
  [ngModel]="filterFromDate()"
  (ngModelChange)="filterFromDate.set($event)" />
```

Same for the "Datum tot" input, using `inputId="filter-to-date"` and `placeholder="Datum tot"`.

Update the component `.ts` file:
- Import `DateInputComponent` from `@org/shared-lib`.
- Add to the component's `imports`.

---

### F5 — Update shift schedule editor (time inputs)

**File:** `libs/chairly/src/lib/staff/ui/shift-schedule-editor/shift-schedule-editor.component.html`

Replace both native `<input type="time">` elements per shift block with `<chairly-date-input>`.

**Before:**
```html
<input
  type="time"
  [value]="block.startTime"
  (change)="updateStartTime(row.key, i, $event)"
  class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-sm" />
```

**After:**
```html
<chairly-date-input
  type="time"
  [ngModel]="block.startTime"
  (ngModelChange)="onStartTimeChange(row.key, i, $event)" />
```

Update the component `.ts` file:
- Import `DateInputComponent` from `@org/shared-lib` and `FormsModule` from `@angular/forms`.
- Add both to the component's `imports`.
- Replace `updateStartTime(day, index, $event: Event)` with `onStartTimeChange(day, index, value: string)` that directly receives the time string.
- Same for `updateEndTime` → `onEndTimeChange`.

---

### F6 — Unit tests for DateInputComponent

**File:** `libs/shared/src/lib/ui/date-input/date-input.component.spec.ts`

Write unit tests covering:

1. **Renders placeholder** when no value is set.
2. **Displays formatted value** — set a date value and verify the trigger shows `DD-MM-YYYY` format.
3. **Opens popover on click** — click the trigger, verify the popover with the native input and buttons is visible.
4. **Confirm commits value** — open popover, change the native input value, click "Bevestigen", verify the value is committed and popover is closed.
5. **Cancel restores value** — open popover, change the native input value, click "Annuleren", verify the original value is restored and popover is closed.
6. **Escape closes without committing** — open popover, change value, press Escape, verify original value is restored.
7. **Click outside closes without committing** — open popover, change value, simulate click outside, verify original value is restored.
8. **Works with reactive forms** — wrap in a form with `formControlName`, verify the value propagates correctly.
9. **Works with ngModel** — use `[(ngModel)]`, verify two-way binding.
10. **Supports all three types** — verify `type="date"`, `type="datetime-local"`, and `type="time"` each render the correct native input type.

---

### F7 — Update existing e2e tests

Existing Playwright e2e tests that interact with date/time inputs will need to be updated since the native inputs are now inside a popover.

The new interaction pattern for e2e tests:
1. Click the trigger button (identified by `id` or label).
2. Wait for the popover to appear.
3. Fill the native input inside the popover.
4. Click the "Bevestigen" button.

Review and update all affected e2e tests in `apps/chairly-e2e/src/`.

---

## Acceptance Criteria

- [ ] A shared `<chairly-date-input>` component exists in `libs/shared/src/lib/ui/date-input/`
- [ ] Component implements `ControlValueAccessor` and works with `formControlName`, `[(ngModel)]`, and `[ngModel]`+`(ngModelChange)`
- [ ] Component supports `type="date"`, `type="datetime-local"`, and `type="time"`
- [ ] Clicking the trigger opens a popover with the native input and "Bevestigen" / "Annuleren" buttons
- [ ] "Bevestigen" commits the selected value and closes the popover
- [ ] "Annuleren" restores the previous value and closes the popover
- [ ] Pressing Escape closes the popover without committing
- [ ] Clicking outside the component closes the popover without committing
- [ ] The trigger displays the value formatted in Dutch convention (`DD-MM-YYYY`, `DD-MM-YYYY HH:mm`, or `HH:mm`)
- [ ] Dark mode styling is correct on both trigger and popover
- [ ] All 6 native date/time inputs across the app are replaced with the shared component
- [ ] All frontend quality checks pass (lint, format, test, build)
- [ ] Existing e2e tests are updated and pass

---

## Out of Scope

- Custom calendar/time picker UI (we keep the native browser picker inside the popover)
- Date range picker as a single component (invoice filters remain two separate inputs)
- Min/max date validation constraints (can be added later)
- Locale-aware input parsing (the native input handles this)
- Backend changes
