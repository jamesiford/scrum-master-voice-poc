import { readFile } from 'node:fs/promises';
import type { IncomingMessage, ServerResponse } from 'node:http';
import { extname, join, normalize } from 'node:path';
import type { CastRoutes } from './castRoutes.js';
import { sendJson } from './castRoutes.js';
import type { TeamsRoutes } from './teamsRoutes.js';

const types: Record<string, string> = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.map': 'application/json',
};

/**
 * Routes bridge HTTP requests. Presentation only: no business logic lives here.
 * @param publicDir - Directory with the browser bundle.
 * @param cast - Cast routes.
 * @returns The request handler.
 */
export function createRouter(
  publicDir: string,
  cast: CastRoutes,
  teams: TeamsRoutes,
): (req: IncomingMessage, res: ServerResponse) => void {
  return (req, res) => {
    void route(req, res, publicDir, cast, teams).catch((error: unknown) => {
      sendJson(res, 500, {
        error: 'bridge_error',
        detail: error instanceof Error ? error.message : 'Unexpected error',
      });
    });
  };
}

async function route(
  req: IncomingMessage,
  res: ServerResponse,
  publicDir: string,
  cast: CastRoutes,
  teams: TeamsRoutes,
): Promise<void> {
  const url = new URL(req.url ?? '/', 'http://localhost');
  const path = url.pathname;

  if (path === '/healthz') return sendJson(res, 200, { status: 'healthy' });
  if (path === '/cast/script' && req.method === 'GET')
    return cast.script(res, url.searchParams.get('kind') ?? undefined);
  if (path === '/cast/line' && req.method === 'POST') return cast.line(req, res);
  if (path === '/acs/token' && req.method === 'POST') return teams.token(res);
  if (path === '/teams/join' && req.method === 'POST') return teams.control(req, res);
  if (path.startsWith('/teams/') && req.method === 'DELETE')
    return teams.control(req, res, decodeURIComponent(path.slice('/teams/'.length)));
  if (path.startsWith('/teams/')) return serveFile(res, join(publicDir, 'teams.html'));
  if (path === '/room' || path.startsWith('/room/'))
    return serveFile(res, join(publicDir, 'room.html'));
  if (path.startsWith('/static/')) {
    const file = normalize(path.slice('/static/'.length));
    return file.startsWith('..')
      ? sendJson(res, 404, { error: 'not_found' })
      : serveFile(res, join(publicDir, file));
  }
  sendJson(res, 404, { error: 'not_found' });
}

async function serveFile(res: ServerResponse, path: string): Promise<void> {
  try {
    const content = await readFile(path);
    res
      .writeHead(200, {
        'content-type': types[extname(path)] ?? 'application/octet-stream',
        'cache-control': 'no-store',
      })
      .end(content);
  } catch {
    sendJson(res, 404, {
      error: 'not_found',
      detail: 'Run "npm run build:web" to build the room page.',
    });
  }
}
