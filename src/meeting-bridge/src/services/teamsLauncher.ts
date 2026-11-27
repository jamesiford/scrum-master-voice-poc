import { chromium, type Browser, type BrowserContext, type Page } from 'playwright-core';

/** What the API asks the bridge to do when it starts a Teams standup. */
export interface JoinRequest {
  sessionId: string;
  joinUrl: string;
  bridgeToken: string;
  apiBase: string;
  displayName: string;
}

/** One live call page. */
interface Call {
  context: BrowserContext;
  page: Page;
}

/**
 * Runs the Teams call page in headless Chromium, one browser context per standup (SPEC 7.3).
 * It only opens the page; the page and the API gateway do everything else.
 */
export class TeamsLauncher {
  private browser: Browser | undefined;
  private readonly calls = new Map<string, Call>();

  /**
   * @param baseUrl - Where this bridge serves the call page.
   * @param executablePath - Chromium path (container); when empty a local Edge or Chrome is used.
   */
  constructor(
    private readonly baseUrl: string,
    private readonly executablePath: string | undefined,
  ) {}

  // Locally an installed Edge is used; the container ships Playwright's own Chromium (PLAYWRIGHT_BROWSERS_PATH).
  private browserChoice():
    { executablePath: string } | { channel: string } | Record<string, never> {
    if (this.executablePath) return { executablePath: this.executablePath };
    return process.env['PLAYWRIGHT_BROWSERS_PATH'] ? {} : { channel: 'msedge' };
  }

  /**
   * Joins a meeting.
   * @param request - Session, meeting link and tokens.
   */
  async join(request: JoinRequest): Promise<void> {
    await this.leave(request.sessionId);
    this.browser ??= await chromium.launch({
      headless: true,
      ...this.browserChoice(),
      args: [
        '--autoplay-policy=no-user-gesture-required',
        '--use-fake-ui-for-media-stream',
        // The ACS calling SDK silently ignores custom audio streams when no microphone
        // exists (azure-sdk-for-js#29174); a fake device satisfies that check.
        '--use-fake-device-for-media-stream',
        '--no-sandbox',
      ],
    });
    const context = await this.browser.newContext();
    const page = await context.newPage();
    // Browser/SDK diagnostics can repeat meeting content or token-bearing URLs; the call page still displays its actual error.
    page.on('console', (m) => {
      if (m.type() === 'warning' || m.type() === 'error')
        console.warn(`[call ${request.sessionId}] browser console severity=${m.type()}`);
    });
    page.on('pageerror', () => console.error(`[call ${request.sessionId}] browser page error`));
    this.calls.set(request.sessionId, { context, page });
    const query = new URLSearchParams({
      token: request.bridgeToken,
      api: request.apiBase,
      join: request.joinUrl,
      name: request.displayName,
    });
    await page.goto(`${this.baseUrl}/teams/${encodeURIComponent(request.sessionId)}?${query}`);
  }

  /**
   * Reads the call page's last status line, for diagnostics.
   * @param sessionId - Session id.
   * @returns The status text, or `undefined` when there is no call.
   */
  async state(sessionId: string): Promise<string | undefined> {
    const call = this.calls.get(sessionId);
    if (!call) return undefined;
    return call.page.evaluate(() => (window as unknown as { __teamsState?: string }).__teamsState);
  }

  /**
   * Leaves a meeting and closes its browser context.
   * @param sessionId - Session id.
   */
  async leave(sessionId: string): Promise<void> {
    const call = this.calls.get(sessionId);
    if (!call) return;
    this.calls.delete(sessionId);
    await call.context.close().catch(() => undefined);
  }
}
