# Smart Component Boilerplate

Location: `libs/chairly/src/lib/{domain}/feature/{entity}-list-page/{entity}-list-page.component.ts`

```typescript
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';

// shared UI imports if needed:
// import { ConfirmationDialogComponent } from '@org/shared-lib';

import { {Entity}Store } from '../../data-access';
import { {Entity}Response, Create{Entity}Request, Update{Entity}Request } from '../../models';
// import { {Entity}FormDialogComponent, {Entity}TableComponent } from '../../ui';

@Component({
  selector: 'chairly-{entity}-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    // {Entity}FormDialogComponent,
    // {Entity}TableComponent,
    // ConfirmationDialogComponent,
  ],
  templateUrl: './{entity}-list-page.component.html',
})
export class {Entity}ListPageComponent implements OnInit {
  private readonly {entity}Store = inject({Entity}Store);

  // private readonly formDialogRef = viewChild.required({Entity}FormDialogComponent);
  // private readonly deleteDialogRef = viewChild.required<ConfirmationDialogComponent>('deleteDialog');

  protected readonly selected{Entity} = signal<{Entity}Response | null>(null);

  protected readonly {entities} = computed<{Entity}Response[]>(() =>
    this.{entity}Store.{entities}(),
  );
  protected readonly isLoading = computed<boolean>(() =>
    this.{entity}Store.isLoading(),
  );

  ngOnInit(): void {
    this.{entity}Store.load{Entities}();
  }

  protected onAdd{Entity}(): void {
    this.selected{Entity}.set(null);
    // this.formDialogRef().open(null);
  }

  protected onEdit{Entity}(item: {Entity}Response): void {
    this.selected{Entity}.set(item);
    // this.formDialogRef().open(item);
  }

  protected onSaved(request: Create{Entity}Request | Update{Entity}Request): void {
    const current = this.selected{Entity}();
    if (current) {
      this.{entity}Store.update{Entity}(current.id, request as Update{Entity}Request);
    } else {
      this.{entity}Store.create{Entity}(request as Create{Entity}Request);
    }
    this.selected{Entity}.set(null);
  }

  protected onDelete{Entity}(item: {Entity}Response): void {
    this.selected{Entity}.set(item);
    // this.deleteDialogRef().open();
  }

  protected onConfirmDelete(): void {
    const item = this.selected{Entity}();
    if (item) {
      this.{entity}Store.delete{Entity}(item.id);
    }
    this.selected{Entity}.set(null);
  }
}
```

## Template (`{entity}-list-page.component.html`)

```html
<div class="p-6">
  <div class="mb-4 flex items-center justify-between">
    <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{Entities}</h1>
    <button
      type="button"
      class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 dark:bg-primary-500 dark:hover:bg-primary-600"
      (click)="onAdd{Entity}()"
    >
      Toevoegen
    </button>
  </div>

  @if (isLoading()) {
    <p class="text-gray-500 dark:text-gray-400">Laden...</p>
  } @else if ({entities}().length === 0) {
    <p class="text-gray-500 dark:text-gray-400">Geen {entities} gevonden.</p>
  } @else {
    <!-- render {entities}() here -->
  }
</div>
```

## Rules

- `ChangeDetectionStrategy.OnPush` — required on all components
- `standalone: true` — no NgModules
- `inject()` for all dependencies — no constructor injection
- `signal()` for local mutable state (`selected{Entity}`, dialog state)
- `computed()` for derived view state from store signals
- `viewChild.required()` for dialog/child component refs — not `@ViewChild`
- `OnInit` is fine; prefer `ngOnInit` for data loading (not constructor)
- Template in separate `.html` file — `templateUrl:` always, never inline `template:`
- Omit `imports: []` when component has no imports — don't leave an empty array
- All UI text in Dutch — no English labels, buttons, or messages
- `@if` / `@else` / `@for` control flow — not `*ngIf` / `*ngFor` directives
- Dark mode: pair every light background with `dark:` variant
  (e.g. `bg-white dark:bg-slate-800`, `text-gray-900 dark:text-white`)
- Custom/brand colors (`bg-primary-*`, `bg-accent-*`) always need explicit `dark:` variant
