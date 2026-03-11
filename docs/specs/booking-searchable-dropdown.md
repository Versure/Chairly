# Booking Searchable Dropdown

## Overview

When creating or editing a booking, the client and staff member fields use native `<select>` dropdowns. As the number of clients and staff members grows, these lists become unwieldy to scroll through. This feature replaces the native selects with searchable combobox/autocomplete inputs so users can quickly find the right person by typing part of their name. Fixes GitHub issue #49.

---

## Domain Context

- **Bounded context:** Bookings (frontend only — no new backend endpoints needed)
- **Key files involved:**
  - `libs/chairly/src/lib/bookings/ui/booking-form-dialog/booking-form-dialog.component.html`
  - `libs/chairly/src/lib/bookings/ui/booking-form-dialog/booking-form-dialog.component.ts`
  - `libs/shared/src/lib/ui/` — new shared searchable dropdown component

---

## Frontend Tasks

### F1 — Create shared searchable dropdown component

Create a reusable `<chairly-searchable-dropdown>` component in the shared library that can be used in any form requiring a search-filtered selection.

**Folder:** `libs/shared/src/lib/ui/searchable-dropdown/`

**Files:**
- `searchable-dropdown.component.ts`
- `searchable-dropdown.component.html`

**Component API:**

```typescript
// Selector: chairly-searchable-dropdown
// ChangeDetectionStrategy.OnPush, standalone
// Implements ControlValueAccessor for reactive form integration

options = input<DropdownOption[]>([]);       // list of { id: string; label: string }
placeholder = input<string>('Zoeken...');
disabled = input<boolean>(false);
```

**Model** (`libs/shared/src/lib/ui/searchable-dropdown/dropdown-option.model.ts`):

```typescript
export interface DropdownOption {
  id: string;
  label: string;
}
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

Export from `libs/shared/src/lib/ui/index.ts`.

### F2 — Replace client and staff member selects in booking form dialog

**File:** `libs/chairly/src/lib/bookings/ui/booking-form-dialog/booking-form-dialog.component.html`

Replace the native `<select>` for `clientId` and `staffMemberId` with `<chairly-searchable-dropdown>`:

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
clientOptions = computed<DropdownOption[]>(() =>
  this.clients().map(c => ({ id: c.id, label: `${c.firstName} ${c.lastName}` }))
);

staffMemberOptions = computed<DropdownOption[]>(() =>
  this.staffMembers().map(m => ({ id: m.id, label: `${m.firstName} ${m.lastName}` }))
);
```

Add `SearchableDropdownComponent` to the component's imports.

### F3 — Unit tests for searchable dropdown component

Write unit tests for `SearchableDropdownComponent`:
- Renders input with placeholder
- Filters options as user types
- Shows "Geen resultaten gevonden" when no match
- Selects option on click and emits correct `id` value
- Keyboard navigation (ArrowDown, Enter, Escape)
- Closes on outside click
- Works correctly with Angular reactive forms (ControlValueAccessor)

---

## Acceptance Criteria

- [ ] A shared `<chairly-searchable-dropdown>` component exists in `libs/shared/src/lib/ui/searchable-dropdown/`
- [ ] Component implements `ControlValueAccessor` for reactive form integration
- [ ] Typing in the input filters the options list case-insensitively
- [ ] Keyboard navigation (ArrowUp/Down, Enter, Escape) works
- [ ] "Geen resultaten gevonden" empty state is shown when no options match
- [ ] Clicking outside closes the dropdown
- [ ] The booking form uses the searchable dropdown for both client and staff member fields
- [ ] Form validation still works (required field, touched state)
- [ ] Component works in both light and dark mode
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Existing booking e2e tests still pass

---

## Out of Scope

- Replacing native `<select>` elements in other forms (services category, etc.) — those lists are short and do not need search
- Virtual scrolling for very large lists
- Server-side search / API filtering (client-side filtering is sufficient for typical salon sizes)
- Multi-select functionality
