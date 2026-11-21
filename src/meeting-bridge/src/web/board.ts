import type { BoardState } from './protocol.js';

const WIDTH = 1280;
const HEIGHT = 720;

/**
 * Renders the standup board onto a canvas and exposes it as a video stream, so the agent can share it as its screen
 * in the Teams meeting. Pure drawing: it holds no conversation logic.
 */
export class BoardRenderer {
  private readonly canvas = document.createElement('canvas');
  private readonly ctx: CanvasRenderingContext2D;
  private state: BoardState | undefined;

  /** Creates the renderer and paints the waiting screen. */
  constructor() {
    this.canvas.width = WIDTH;
    this.canvas.height = HEIGHT;
    const ctx = this.canvas.getContext('2d');
    if (!ctx) throw new Error('Canvas 2D is not available.');
    this.ctx = ctx;
    this.draw();
    // A steady repaint keeps the captured stream producing frames even when nothing changes.
    setInterval(() => this.draw(), 1000);
  }

  /**
   * Starts capturing the canvas.
   * @returns A video stream of the board.
   */
  stream(): MediaStream {
    return this.canvas.captureStream(5);
  }

  /**
   * Replaces the board contents.
   * @param state - The latest board state from the gateway.
   */
  update(state: BoardState): void {
    this.state = state;
    this.draw();
  }

  private draw(): void {
    const { ctx } = this;
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(0, 0, WIDTH, HEIGHT);
    const s = this.state;
    this.drawPulse();
    ctx.fillStyle = '#e2e8f0';
    ctx.font = '600 34px Segoe UI, Arial, sans-serif';
    ctx.fillText(s?.speaker ? `${s.speaker}'s items` : 'Standup agenda: flagged items', 48, 70);
    ctx.fillStyle = '#94a3b8';
    ctx.font = '20px Segoe UI, Arial, sans-serif';
    if (!s) {
      ctx.fillText('Preparing the sprint board...', 48, 110);
      return;
    }
    ctx.fillText(
      `${s.sprint} · ${s.daysLeft} days left${s.goal ? ' · Goal: ' + clip(s.goal, 70) : ''}`,
      48,
      108,
    );
    if (s.ended) {
      ctx.fillStyle = '#4ade80';
      ctx.font = '600 28px Segoe UI, Arial, sans-serif';
      ctx.fillText('Standup complete. Follow-ups are in Teams for approval.', 48, 190);
    } else {
      this.drawItems(s);
    }
    this.drawTeam(s);
  }

  // A static canvas yields no video frames, so Teams shows nothing until something changes. A ticking clock keeps frames flowing.
  private drawPulse(): void {
    const { ctx } = this;
    ctx.fillStyle = '#64748b';
    ctx.font = '16px Segoe UI, Arial, sans-serif';
    ctx.fillText(new Date().toLocaleTimeString(), WIDTH - 140, 36);
  }

  private drawItems(s: BoardState): void {
    const { ctx } = this;
    if (s.items.length === 0) {
      ctx.fillStyle = '#94a3b8';
      ctx.font = '24px Segoe UI, Arial, sans-serif';
      ctx.fillText('No items assigned.', 48, 190);
      return;
    }
    s.items.slice(0, 6).forEach((item, i) => {
      const y = 140 + i * 84;
      ctx.fillStyle = item.covered ? '#14532d' : '#1e293b';
      ctx.fillRect(48, y, 860, 72);
      if (item.id === s.focus) {
        ctx.strokeStyle = '#38bdf8';
        ctx.lineWidth = 3;
        ctx.strokeRect(48, y, 860, 72);
      }
      ctx.fillStyle = item.covered ? '#4ade80' : item.flags.length ? '#fbbf24' : '#64748b';
      ctx.fillRect(48, y, 8, 72);
      ctx.fillStyle = '#f8fafc';
      ctx.font = '600 22px Segoe UI, Arial, sans-serif';
      ctx.fillText(`${item.covered ? '✓ ' : ''}#${item.id}  ${clip(item.title, 52)}`, 72, y + 30);
      ctx.fillStyle = '#94a3b8';
      ctx.font = '18px Segoe UI, Arial, sans-serif';
      const flags = item.flags.length ? ` · ${item.flags.join(', ')}` : '';
      const owner = item.owner && !s.speaker ? ` · ${item.owner}` : '';
      ctx.fillText(`${item.type} · ${item.state}${flags}${owner}`, 72, y + 56);
    });
  }

  private drawTeam(s: BoardState): void {
    const { ctx } = this;
    ctx.fillStyle = '#e2e8f0';
    ctx.font = '600 22px Segoe UI, Arial, sans-serif';
    ctx.fillText('Coverage', 948, 160);
    s.team.forEach((m, i) => {
      const y = 200 + i * 56;
      ctx.fillStyle = m.name === s.speaker ? '#38bdf8' : '#cbd5e1';
      ctx.font = '20px Segoe UI, Arial, sans-serif';
      ctx.fillText(`${m.name}  ${m.covered}/${m.total}`, 948, y);
      ctx.fillStyle = '#1e293b';
      ctx.fillRect(948, y + 10, 280, 10);
      ctx.fillStyle = '#4ade80';
      ctx.fillRect(948, y + 10, m.total ? (280 * m.covered) / m.total : 0, 10);
    });
  }
}

function clip(text: string, max: number): string {
  return text.length > max ? text.slice(0, max - 1) + '…' : text;
}
