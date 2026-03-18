---
name: frontend-test
description: >
  Vitest and Playwright test patterns for Chairly frontend.
  Use when writing unit tests for components or e2e tests for features.
---

# Frontend Test Patterns

## Vitest — Component unit test

Location: `libs/chairly/src/lib/{domain}/ui/{component-name}/{component-name}.component.spec.ts`

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { {ComponentName}Component } from './{component-name}.component';

describe('{ComponentName}Component', () => {
  let component: {ComponentName}Component;
  let fixture: ComponentFixture<{ComponentName}Component>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [{ComponentName}Component],
    }).compileComponents();

    fixture = TestBed.createComponent({ComponentName}Component);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

## Playwright — E2E test

Location: `apps/chairly-e2e/src/{domain}.spec.ts`

```typescript
import { test, expect } from '@playwright/test';

test.describe('{Domain} page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/{domain}');
  });

  test('should display the page title', async ({ page }) => {
    // All assertions against Dutch text
    await expect(page.getByRole('heading', { name: '{Dutch title}' })).toBeVisible();
  });

  test('should load items', async ({ page }) => {
    await expect(page.getByRole('table')).toBeVisible();
  });
});
```

## Rules

- All e2e test assertions use Dutch UI text
- Use `page.keyboard.press('Escape')` to close `showModal()` dialogs (cross-browser reliable)
- E2E tests go in `apps/chairly-e2e/src/`
- Unit tests go alongside the component file
- Test naming: `should {expected behavior}`

## Running tests

```bash
# Unit tests
cd src/frontend/chairly
npx nx affected -t test --base=main

# E2E tests
npx nx affected -t e2e --base=main
```
