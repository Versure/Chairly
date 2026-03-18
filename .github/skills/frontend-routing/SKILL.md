---
name: frontend-routing
description: >
  Route configuration patterns for Chairly frontend domains.
  Use when setting up lazy-loaded routes and domain route files.
---

# Routing Patterns

## Domain routes file

Location: `libs/chairly/src/lib/{domain}/{domain}.routes.ts` (at domain root, NOT inside feature/)

```typescript
import { Route } from '@angular/router';

import { {Entity}ApiService, {Entity}Store } from './data-access';
import { {Entity}ListPageComponent } from './feature';

export const {domain}Routes: Route[] = [
  {
    path: '',
    component: {Entity}ListPageComponent,
    providers: [{Entity}Store, {Entity}ApiService],
  },
];
```

## Register in app router

In `apps/chairly/src/app/app.routes.ts`:

```typescript
export const appRoutes: Route[] = [
  {
    path: '{domain}',
    loadChildren: () =>
      import('@org/chairly-lib').then((m) => m.{domain}Routes),
  },
];
```

## Rules

- Routes file lives at domain root, not inside `feature/`
- Lazy-loaded routes per domain
- Store and API service provided at route level
- Each route component gets its own `feature/{feature-name}/` subfolder

## Feature barrel export

Location: `libs/chairly/src/lib/{domain}/feature/index.ts`

```typescript
export { {Entity}ListPageComponent } from './{entity}-list-page/{entity}-list-page.component';
```
