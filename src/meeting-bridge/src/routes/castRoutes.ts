import type { IncomingMessage, ServerResponse } from 'node:http';
import { CastPlanner, type PlannedLine } from '../domain/cast.js';
import type { SpeechSynthesizer } from '../services/speech.js';
import type { CastScript } from '../domain/cast.js';

/** A cast line with its audio, as sent to the Simulated Room. */
export interface SpokenLine {
  memberId: string;
  text: string;
  /** Base64 PCM16 24 kHz mono, or `null` when speech synthesis failed (the room then shows the text only). */
  audio: string | null;
  error?: string;
}

/** Response of `POST /cast/line`. */
export interface CastLineResponse {
  line: SpokenLine | null;
  sidebar: SpokenLine[];
}

/**
 * Serves cast scripts and lines for the Simulated Room. Planning is the pure {@link CastPlanner}; this class adds I/O.
 */
export class CastRoutes {
  private readonly planners = new Map<string, CastPlanner>();

  /**
   * @param loadScript - Loads the cast file (re-read per session so edits apply).
   * @param speech - Speech synthesis.
   */
  constructor(
    private readonly loadScript: () => Promise<CastScript>,
    private readonly speech: SpeechSynthesizer,
  ) {}

  /**
   * Handles `GET /cast/script`: who is in the cast and who is absent (no lines, no audio).
   * @param res - Response.
   */
  async script(res: ServerResponse): Promise<void> {
    const script = await this.loadScript();
    sendJson(res, 200, {
      members: script.members.map((m) => ({
        memberId: m.memberId,
        voice: m.voice,
        absent: m.absent === true,
      })),
    });
  }

  /**
   * Handles `POST /cast/line` with `{ sessionId, memberId, question }`.
   * @param req - Request.
   * @param res - Response.
   */
  async line(req: IncomingMessage, res: ServerResponse): Promise<void> {
    const body = JSON.parse(await readBody(req)) as {
      sessionId?: string;
      memberId?: string;
      question?: string;
    };
    if (!body.sessionId || !body.memberId) {
      sendJson(res, 400, { error: 'bad_request', detail: 'sessionId and memberId are required.' });
      return;
    }

    const planner = await this.plannerFor(body.sessionId);
    const planned = planner.next(body.memberId, body.question ?? '');
    const sidebar = planned ? planner.takeSidebar(body.memberId) : [];
    const [line, ...side] = await Promise.all(
      [planned, ...sidebar].map((p) => (p ? this.speak(p) : Promise.resolve(null))),
    );
    const result: CastLineResponse = {
      line: line ?? null,
      sidebar: side.filter((s): s is SpokenLine => s !== null),
    };
    sendJson(res, 200, result);
  }

  private async plannerFor(sessionId: string): Promise<CastPlanner> {
    let planner = this.planners.get(sessionId);
    if (!planner) {
      planner = new CastPlanner(await this.loadScript());
      this.planners.set(sessionId, planner);
    }
    return planner;
  }

  private async speak(line: PlannedLine): Promise<SpokenLine> {
    try {
      const pcm = await this.speech.synthesize(line.voice, line.text);
      return { memberId: line.memberId, text: line.text, audio: pcm.toString('base64') };
    } catch (error) {
      return {
        memberId: line.memberId,
        text: line.text,
        audio: null,
        error: error instanceof Error ? error.message : 'Speech failed',
      };
    }
  }
}

/**
 * Writes a JSON response.
 * @param res - Response.
 * @param status - HTTP status.
 * @param body - Value to serialise.
 */
export function sendJson(res: ServerResponse, status: number, body: unknown): void {
  res.writeHead(status, { 'content-type': 'application/json' }).end(JSON.stringify(body));
}

async function readBody(req: IncomingMessage): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) chunks.push(chunk as Buffer);
  return Buffer.concat(chunks).toString('utf8');
}
