import { defineConfig, devices } from '@playwright/test';

// The whole stack, started for the test run:
//   · API (Kestrel) on :5091 — Development, so it CORS-allows the client and seeds an empty DB.
//   · Angular dev server on :4200 — its environment.development apiUrl points at the API above.
// The API borrows the same connection string the integration tests use (DISTRICT_SQL_TEST).
const apiUrl = 'http://localhost:5091';
const appUrl = 'http://localhost:4200';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: appUrl,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../src/Api --no-launch-profile',
      url: `${apiUrl}/health`,
      timeout: 120_000,
      reuseExistingServer: false, // always start OUR stack — never adopt a stray server on the port
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: apiUrl,
        ConnectionStrings__Sql: process.env.DISTRICT_SQL_TEST ?? '',
        Seed__OnStartup: 'true', // this run uses --no-launch-profile, so opt in explicitly
      },
    },
    {
      command: 'npm start',
      cwd: '../web',
      url: appUrl,
      timeout: 120_000,
      reuseExistingServer: false, // always start OUR stack — never adopt a stray server on the port
    },
  ],
});
