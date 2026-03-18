---
name: frontend-component
description: >
  Angular component patterns for Chairly frontend (smart and presentational).
  Use when creating new components with signals, OnPush, and templateUrl.
---

# Component Patterns

## Smart (container) component

Location: `libs/chairly/src/lib/{domain}/feature/{feature-name}/{feature-name}.component.ts`

```typescript
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';

import { {Entity}Store } from '../../data-access';

@Component({
  selector: 'chairly-{feature-name}',
  templateUrl: './{feature-name}.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class {FeatureName}Component implements OnInit {
  private readonly store = inject({Entity}Store);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = this.store.items;
  readonly loading = this.store.loading;

  ngOnInit(): void {
    this.store.loadAll();
  }
}
```

## Presentational (dumb) component

Location: `libs/chairly/src/lib/{domain}/ui/{component-name}/{component-name}.component.ts`

```typescript
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { {Entity}Response } from '../../models';

@Component({
  selector: 'chairly-{component-name}',
  templateUrl: './{component-name}.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class {ComponentName}Component {
  readonly items = input.required<{Entity}Response[]>();
  readonly itemSelected = output<{Entity}Response>();
}
```

## Rules

- Always use `templateUrl:` with a separate `.html` file (no inline template)
- Always use `ChangeDetectionStrategy.OnPush`
- Standalone components (no NgModules)
- Signal-based APIs: `input()`, `model()`, `viewChild()`, `output()`
- Omit `imports: []` when component has no imports
- Prefix: `chairly-`
- All UI text in Dutch
- Pair every light-mode color with `dark:` Tailwind variant
- Every presentational component in its own `ui/{component-name}/` subfolder
- Use `takeUntilDestroyed(destroyRef)` for subscriptions (inject `DestroyRef` explicitly)

## Native `<dialog>` pattern

```html
<dialog
  class="fixed inset-0 m-0 w-screen h-screen max-w-none max-h-none flex items-center justify-center border-0 bg-black/50 p-4"
>
  <div class="bg-white dark:bg-slate-800 rounded-lg shadow-xl w-full max-w-md mx-auto">
    <!-- content -->
  </div>
</dialog>
```

Inject `DOCUMENT`, toggle `document.body.style.overflow` on open/close.
