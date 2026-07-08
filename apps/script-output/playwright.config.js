// @ts-check
const path = require('path');

const repoRoot = path.resolve(__dirname, '../..');
const e2eDb = path.join(__dirname, 'e2e/fixtures/e2e.db');

const e2ePort = process.env.CONTINUUUUM_E2E_PORT || '5051';
const e2eBase = `http://127.0.0.1:${e2ePort}`;

/** @type {import('@playwright/test').PlaywrightTestConfig} */
module.exports = {
  testDir: './e2e',
  timeout: 90_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list']],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || e2eBase,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  webServer: process.env.PLAYWRIGHT_SKIP_SERVER
    ? undefined
    : {
        command: `python "${path.join(__dirname, 'e2e/start_e2e_server.py')}"`,
        cwd: path.join(repoRoot, 'apps', 'script-output'),
        url: `${e2eBase}/script-output`,
        reuseExistingServer: false,
        timeout: 120_000,
      },
};
