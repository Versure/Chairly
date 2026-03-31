# Booking Searchable Dropdown

> **Status: Implemented** — Merged to main.

## Overview

When creating or editing a booking, the client and staff member fields currently use native `<select>` dropdowns. As the number of clients and staff members grows, these lists become unwieldy to scroll through. This feature replaces the native selects with searchable combobox/autocomplete inputs so users can quickly find the right person by typing part of their name. This is a **frontend-only** feature -- no new backend endpoints are needed. Fixes GitHub issue #49.

## Domain Context

- Bounded context: Bookings (frontend only)
- Key entities involved: Booking (form dialog), Client (selection), StaffMember (selection)
- Ubiquitous language: Booking (never "appointment"), Client (never "customer"), Staff Member (never "employee")
- Key files involved:
  - `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.html`
  - `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.ts`
  - `libs/shared/src/lib/ui/` -- new shared searchable dropdown component

## Frontend Tasks

### F1 -- Create shared searchable dropdown component

Create a reusable `<chairly-searchable-dropdown>` component in the shared library that can be used in any form requiring a search-filtered selection.

**Folder:** `libs/shared/src/lib/ui/searchable-dropdown/`

**Files:**
- `searchable-dropdown.component.ts`
- `searchable-dropdown.component.html`

**Model** (`libs/shared/src/lib/ui/searchable-dropdown/dropdown-option.model.ts`):

```typescript
export interface DropdownOption {
  id: string;
  label: string;
}
```

**Component API:**

```typescript
// Selector: chairly-searchable-dropdown
// ChangeDetectionStrategy.OnPush, standalone
// Implements ControlValueAccessor for reactive form integration

options = input<DropdownOption[]>([]);       // list of { id: string; label: string }
placeholder = input<string>('Zoeken...');
disabled = input<boolean>(false);
```

**Behaviour:**
- Renders a text `<input>` that filters the options list as the user types (case-insensitive, matches anywhere in label)
- Below the input, a dropdown panel appears (positioned absolutely) showing matched options
- Clicking an option selects it: the input shows the selected label, the form control value is set to the option's `id`
- Keyboard navigation: `ArrowDown`/`ArrowUp` to move through options, `Enter` to select, `Escape` to close
- Clicking outside the component closes the dropdown panel
- When a value is already selected and the user starts typing, the dropdown re-opens with filtered results, clearing the previous selection until a new one is chosen
- Empty state: "Geen resultaten gevonden" when filter matches nothing
- The component uses `HostListener` for document click detection to close on outside click
- Integrates with Angular reactive forms via `ControlValueAccessor`
- Must use `forwardRef(() => SearchableDropdownComponent)` in the `NG_VALUE_ACCESSOR` provider
- Must properly call `onTouched()` when the dropdown closes or a selection is made
- Must respect the `disabled` input for form disable state

**Template structure:**

```html
<div class="relative">
  <input
    type="text"
    [placeholder]="placeholder()"
    [disabled]="disabled()"
    class="w-full rounded border border-gray-300 dark:border-slate-600 px-3 py-2 text-sm
           bg-white dark:bg-slate-700 text-gray-900 dark:text-slate-100
           focus:outline-none focus:ring-2 focus:ring-primary-500"
  />
  @if (isOpen()) {
    <ul class="absolute z-10 mt-1 w-full max-h-60 overflow-y-auto
               bg-white dark:bg-slate-700 border border-gray-200 dark:border-slate-600
               rounded shadow-lg text-sm">
      @for (option of filteredOptions(); track option.id) {
        <li class="px-3 py-2 cursor-pointer hover:bg-primary-50 dark:hover:bg-slate-600
                   aria-selected:bg-primary-100">
          {{ option.label }}
        </li>
      }
      @if (filteredOptions().length === 0) {
        <li class="px-3 py-2 text-gray-400 dark:text-slate-400">Geen resultaten gevonden</li>
      }
    </ul>
  }
</div>
```

**Dark mode:** The template includes explicit `dark:` Tailwind variants for all custom/brand color classes. Light-mode backgrounds (`bg-white`, `hover:bg-primary-50`) are paired with dark equivalents (`dark:bg-slate-700`, `dark:hover:bg-slate-600`).

**Export:** Add `SearchableDropdownComponent` and `DropdownOption` to `libs/shared/src/lib/ui/index.ts`.

### F2 -- Replace client and staff member selects in booking form dialog

**Files:**
- `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.html`
- `libs/chairly/src/lib/bookings/ui/booking-form-dialog.component.ts`

Replace the native `<select>` elements for `clientId` and `staffMemberId` with `<chairly-searchable-dropdown>`:

```html
<!-- Client -->
<chairly-searchable-dropdown
  formControlName="clientId"
  [options]="clientOptions()"
  placeholder="Klant zoeken..."
/>

<!-- Staff member -->
<chairly-searchable-dropdown
  formControlName="staffMemberId"
  [options]="staffMemberOptions()"
  placeholder="Medewerker zoeken..."
/>
```

**In the component TypeScript**, add computed signals that map the existing `clients()` and `staffMembers()` signals to `DropdownOption[]`:

```typescript
import { computed } from '@angular/core';
import { DropdownOption, SearchableDropdownComponent } from '@org/shared-lib';

clientOptions = computed<DropdownOption[]>(() =>
  this.clients().map(c => ({ id: c.id, label: `${c.firstName} ${c.lastName}` }))
);

staffMemberOptions = computed<DropdownOption[]>(() =>
  this.staffMembers().map(m => ({ id: m.id, label: `${m.firstName} ${m.lastName}` }))
);
```

Add `SearchableDropdownComponent` to the component's `imports` array (alongside existing `ReactiveFormsModule` and `SetHasPipe`).

**Preserve existing behaviour:**
- Form validation still works (`Validators.required` on `clientId` and `staffMemberId`)
- The `open()` method still pre-fills the form when editing -- the `ControlValueAccessor.writeValue()` will receive the existing `clientId`/`staffMemberId` and the component must display the matching label
- Validation error messages remain: "Klant is verplicht." and "Medewerker is verplicht."
- Labels "Klant" and "Medewerker" remain above the fields

### F3 -- Unit tests for searchable dropdown component

**File:** `libs/shared/src/lib/ui/searchable-dropdown/searchable-dropdown.component.spec.ts`

Write Vitest unit tests for `SearchableDropdownComponent` covering:

1. **Renders input with placeholder** -- the input element displays the configured placeholder text
2. **Filters options as user types** -- typing a partial name filters the list case-insensitively
3. **Shows "Geen resultaten gevonden" when no match** -- empty state is visible when the filter matches nothing
4. **Selects option on click and emits correct `id` value** -- clicking an option updates the form control value to the option's `id` and displays the option's `label` in the input
5. **Keyboard navigation** -- `ArrowDown` moves highlight down, `ArrowUp` moves highlight up, `Enter` selects the highlighted option, `Escape` closes the dropdown
6. **Closes on outside click** -- clicking outside the component closes the dropdown panel
7. **Works correctly with Angular reactive forms (ControlValueAccessor)** -- setting a form control value programmatically displays the correct label; reading the form control value after selection returns the correct `id`
8. **writeValue displays the matching label** -- when `writeValue` is called with an existing `id`, the input displays the matching option's label (for edit mode)
9. **Respects disabled state** -- when disabled, the input is not interactive

### F4 -- Update Playwright e2e tests for searchable dropdown

**File:** `apps/chairly-e2e/src/bookings.spec.ts`

The existing e2e tests use `selectOption()` on native `<select>` elements and check for `<option>` counts. These must be updated to work with the new searchable dropdown component.

**Changes needed:**

1. **"clicking Nieuwe boeking opens the booking form dialog with dropdowns"** test:
   - Remove assertions checking `option` element counts (there are no `<option>` elements anymore)
   - Instead, verify that the searchable dropdown inputs are visible and can be interacted with
   - Verify typing in the client field shows filtered results
   - Verify the "Geen resultaten gevonden" empty state when typing a non-matching string

2. **"creating a new booking calls the API and refreshes the list"** test:
   - Replace `dialog.getByLabel('Klant').selectOption('client-2')` with: focus the client searchable dropdown input, type part of the name (e.g. "Piet"), click the matching option in the dropdown panel
   - Replace `dialog.getByLabel('Medewerker').selectOption('staff-2')` with the same pattern for staff member

3. **"clicking a booking row opens the edit dialog pre-filled and saves changes"** test:
   - Replace `toHaveValue('client-1')` assertion with checking that the input displays "Jan Jansen"
   - Replace `toHaveValue('staff-1')` assertion with checking that the input displays "Anna de Vries"

**Important:** The `getByLabel('Klant')` selector should still work because the `<label>` element is kept. If not, use the placeholder text or a `data-testid` attribute.

## Acceptance Criteria

- [ ] A shared `<chairly-searchable-dropdown>` component exists in `libs/shared/src/lib/ui/searchable-dropdown/`
- [ ] `DropdownOption` interface is exported from `libs/shared/src/lib/ui/searchable-dropdown/dropdown-option.model.ts`
- [ ] Component implements `ControlValueAccessor` for reactive form integration
- [ ] Typing in the input filters the options list case-insensitively
- [ ] Keyboard navigation (ArrowUp/Down, Enter, Escape) works
- [ ] "Geen resultaten gevonden" empty state is shown when no options match
- [ ] Clicking outside closes the dropdown
- [ ] The booking form uses the searchable dropdown for both client and staff member fields
- [ ] Form validation still works (required field, touched state)
- [ ] Editing a booking pre-fills the dropdown with the current client/staff member name
- [ ] Component works in both light and dark mode
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)
- [ ] Unit tests pass for SearchableDropdownComponent
- [ ] Updated Playwright e2e tests pass
- [ ] Existing booking e2e tests still pass (with updated selectors)

## Out of Scope

- Replacing native `<select>` elements in other forms (services category, staff filter on booking list page, etc.) -- those lists are short and do not need search
- Virtual scrolling for very large lists (100+ options)
- Server-side search / API filtering -- client-side filtering is sufficient for typical salon sizes
- Multi-select functionality
- Debouncing the filter input (not needed for client-side filtering)
- Accessibility (ARIA attributes beyond basic keyboard navigation) -- can be improved in a follow-up
