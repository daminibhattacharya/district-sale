import { expect, test } from '@playwright/test';

/**
 * The golden journey, end to end against the real stack: open the app, pick a district, add a
 * secondary, verify it appears, remove it, verify it's gone. Uses seeded data (North Denmark, with
 * Freja Møller not yet assigned there); the add-then-remove is symmetric, so the test is re-runnable.
 */
test('add and remove a secondary salesperson on a district', async ({ page }) => {
  await page.goto('/');

  // Pick a district from the list.
  await page.getByRole('button', { name: /North Denmark/ }).click();

  // Its coverage loads.
  await expect(page.getByRole('heading', { name: 'North Denmark' })).toBeVisible();

  // A secondary row for a given salesperson (scoped to the secondaries list, not the add dropdown).
  const secondaryRow = (name: string) => page.locator('.rows li', { hasText: name });

  // Freja is not a secondary here yet.
  await expect(secondaryRow('Freja Møller')).toHaveCount(0);

  // Add her as a secondary.
  await page.getByLabel('Add secondary').selectOption({ label: 'Freja Møller' });
  await page.getByRole('button', { name: 'Add', exact: true }).click();

  // She now appears.
  await expect(secondaryRow('Freja Møller')).toHaveCount(1);

  // Remove her again.
  await secondaryRow('Freja Møller').getByRole('button', { name: 'remove' }).click();

  // She's gone.
  await expect(secondaryRow('Freja Møller')).toHaveCount(0);
});
