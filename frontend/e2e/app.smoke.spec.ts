import { expect, test } from '@playwright/test';

test('anonymous member journey renders without browser failures', async ({ page }) => {
  const browserErrors: string[] = [];
  const failedRequests: string[] = [];
  const errorResponses: Array<{ status: number; url: string }> = [];
  page.on('console', (message) => {
    if (message.type() === 'error') {
      browserErrors.push(message.text());
    }
  });
  page.on('pageerror', (error) => browserErrors.push(error.message));
  page.on('requestfailed', (request) => {
    failedRequests.push(`${request.method()} ${request.url()}: ${request.failure()?.errorText}`);
  });
  page.on('response', (response) => {
    if (response.status() >= 400) {
      errorResponses.push({ status: response.status(), url: response.url() });
    }
  });

  await page.goto('./');

  await expect(page).toHaveURL(/\/app\/sign-in$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Return to your communities.');
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();

  await page.getByRole('link', { name: 'Create an account' }).click();
  await expect(page).toHaveURL(/\/app\/register$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    'Create an account, then find your people.',
  );
  const emailBounds = await page.getByLabel('Email').boundingBox();
  expect(emailBounds?.width).toBeGreaterThan(300);

  expect(failedRequests).toEqual([]);
  expect(errorResponses).toHaveLength(1);
  expect(errorResponses[0]?.status).toBe(401);
  expect(errorResponses[0]?.url).toMatch(/\/api\/me$/);
  expect(
    browserErrors.filter(
      (message) =>
        message !==
        'Failed to load resource: the server responded with a status of 401 (Unauthorized)',
    ),
  ).toEqual([]);
});
