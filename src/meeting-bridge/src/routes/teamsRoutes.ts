import type { IncomingMessage, ServerResponse } from 'node:http';
import type { AcsTokenService } from '../services/acs.js';
import type { JoinRequest, TeamsLauncher } from '../services/teamsLauncher.js';
import { sendJson } from './castRoutes.js';

async function readJson<T>(req: IncomingMessage): Promise<T> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) chunks.push(chunk as Buffer);
  return JSON.parse(Buffer.concat(chunks).toString('utf8')) as T;
}

/** HTTP routes for the Teams call page and the API-to-bridge control calls. */
export class TeamsRoutes {
  /**
   * @param launcher - Starts and stops calls.
   * @param acs - ACS token service, if configured.
   * @param apiKey - Shared secret the API sends in `x-bridge-key`; control calls are refused when it is empty.
   */
  constructor(
    private readonly launcher: TeamsLauncher,
    private readonly acs: AcsTokenService | undefined,
    private readonly apiKey: string,
  ) {}

  /**
   * Handles `POST /acs/token`. The call page is the only caller, so it needs no extra credential beyond reaching the bridge.
   * @param res - Response.
   */
  async token(res: ServerResponse): Promise<void> {
    if (!this.acs)
      return sendJson(res, 503, {
        error: 'acs_not_configured',
        detail: 'Set ACS_CONNECTION_STRING or Acs__Endpoint on the bridge.',
      });
    sendJson(res, 200, await this.acs.issue());
  }

  /**
   * Handles `POST /teams/join` and `DELETE /teams/{id}`.
   * @param req - Request.
   * @param res - Response.
   * @param sessionId - Session id for DELETE; undefined for join.
   */
  async control(req: IncomingMessage, res: ServerResponse, sessionId?: string): Promise<void> {
    if (!this.apiKey || req.headers['x-bridge-key'] !== this.apiKey)
      return sendJson(res, 401, { error: 'unauthorized', detail: 'Missing or wrong bridge key.' });
    if (req.method === 'DELETE' && sessionId) {
      await this.launcher.leave(sessionId);
      return sendJson(res, 200, { ok: true });
    }
    const body = await readJson<JoinRequest>(req);
    try {
      await this.launcher.join(body);
      sendJson(res, 202, { ok: true });
    } catch (e) {
      sendJson(res, 502, {
        error: 'browser_failed',
        detail: e instanceof Error ? e.message : 'Could not start the call browser.',
      });
    }
  }
}
