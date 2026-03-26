import { expect, test } from '@playwright/test';

const pages = [
  { name: 'Landing', path: '/' },
  { name: 'Prijzen', path: '/prijzen' },
  { name: 'Abonneren', path: '/abonneren' },
  { name: 'Bevestiging', path: '/bevestiging' },
];

test.describe('Footer sticky positioning', () => {
  for (const { name, path } of pages) {
    test(`${name} page: footer should be at or below viewport bottom`, async ({ page }) => {
      // Mock API calls as needed (plans API for prijzen/abonneren pages)
      await page.route('**/api/onboarding/plans', (route) =>
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        }),
      );

      await page.goto(path);
      await page.waitForLoadState('domcontentloaded');

      const footer = page.locator('chairly-web-footer');
      await expect(footer).toBeVisible();

      // Scroll the footer into view first — boundingBox() returns null for
      // elements outside the viewport (e.g. on long pages where the footer
      // is below the fold).
      await footer.scrollIntoViewIfNeeded();

      const footerBox = await footer.boundingBox();
      const viewportSize = page.viewportSize();

      // The bottom edge of the footer should be at or below the viewport bottom
      expect(footerBox).not.toBeNull();
      expect(viewportSize).not.toBeNull();
      expect(footerBox!.y + footerBox!.height).toBeGreaterThanOrEqual(viewportSize!.height);
    });
  }
});
