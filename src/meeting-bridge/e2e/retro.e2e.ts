import assert from 'node:assert/strict';
import { after, before, describe, it } from 'node:test';
import { chromium, type Browser, type Page } from 'playwright-core';

/**
 * M9b demo check as an automated test: a scripted Simulated Room retrospective (docs/CEREMONIES.md C4).
 * The cast (demo/cast/payments-retro.yaml) gives notes in all three columns, one unclear note that triggers the clarifying
 * question, one step-away, one self-correction and three actions with owners. Floor control, the retrospective agenda, the
 * rule-based sorting and the follow-up all run for real; nothing is written to Azure DevOps (the follow-up stays pending).
 *
 * Needs the stack running (the API on :5080 and the bridge on :5090), `az login`, a seeded sandbox and Microsoft Edge.
 * Run with `npm run e2e:retro`.
 */

const api = process.env['E2E_API'] ?? 'http://localhost:5080';
const TEAM = 'team-payments';

interface SessionDetail {
  status: string;
  kind: string;
  standupMinutes: number;
  events: {
    type: string;
    memberId: string | null;
    text: string;
    category: string | null;
    ownerMemberId: string | null;
    retracted: boolean;
  }[];
}

interface FollowUp {
  sessionId: string;
  kind: string;
  status: string;
  forScrumMaster: boolean;
  changes: { op: string; new?: { type: string; tags: string[]; assignedTo?: string } }[];
}

async function post<T>(path: string, body: unknown = {}): Promise<T> {
  const response = await fetch(api + path, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(body),
  });
  assert.ok(response.ok, `${path} -> ${response.status} ${await response.clone().text()}`);
  return (await response.json().catch(() => undefined)) as T;
}

describe('Simulated Room retrospective', () => {
  let browser: Browser;
  let page: Page;
  let captions: string[] = [];
  let session: SessionDetail;
  let followUp: FollowUp | undefined;
  let elapsedMs = 0;

  before(async () => {
    await post(`/api/teams/${TEAM}/members/sync`);
    const sessionId = await post<string>('/api/retrospective/prepare', { teamId: TEAM });
    const start = await post<{ roomUrl: string }>(`/api/sessions/${sessionId}/start`, {
      mode: 'simulated',
    });

    browser = await chromium.launch({
      channel: 'msedge',
      args: ['--autoplay-policy=no-user-gesture-required'],
    });
    page = await browser.newPage();
    await page.goto(`${start.roomUrl}&autopilot=1`);
    await page.waitForFunction(
      () => !(document.getElementById('start') as HTMLButtonElement).disabled,
      null,
      { timeout: 30_000 },
    );
    const began = Date.now();
    await page.getByRole('button', { name: 'Start standup' }).click();
    await page.waitForFunction(
      () => document.getElementById('floor')?.textContent?.startsWith('Closed'),
      null,
      { timeout: 15 * 60_000 },
    );
    elapsedMs = Date.now() - began;

    captions = await page.locator('.caption').allInnerTexts();
    session = (await (await fetch(`${api}/api/sessions/${sessionId}`)).json()) as SessionDetail;
    // The follow-up is created by a notification handler right after the session ends.
    for (let i = 0; i < 20 && !followUp; i++) {
      const all = (await (await fetch(`${api}/api/followups?teamId=${TEAM}`)).json()) as FollowUp[];
      followUp = all.find((f) => f.sessionId === sessionId);
      if (!followUp) await new Promise((r) => setTimeout(r, 500));
    }
  });

  after(async () => {
    await browser?.close();
  });

  const agentLines = () =>
    captions.filter((c) => c.startsWith('Scrum Agent')).map((c) => c.replace(/^Scrum Agent\n/, ''));
  const notes = () => session.events.filter((e) => e.type === 'RetroItem' && !e.retracted);

  it('is a retrospective that finishes within the timebox', () => {
    assert.equal(session.kind, 'Retrospective');
    assert.equal(session.status, 'Ended');
    assert.ok(
      elapsedMs <= session.standupMinutes * 60_000,
      `took ${Math.round(elapsedMs / 1000)} s`,
    );
  });

  it('collects notes in all three categories', () => {
    for (const category of ['Start', 'Stop', 'Continue'])
      assert.ok(
        notes().some((n) => n.category === category),
        `no ${category} note`,
      );
  });

  it('asks the clarifying question for the unclear note and sorts it from the answer', () => {
    assert.ok(agentLines().some((l) => /start, stop|stop or keep|keep doing/i.test(l)));
    assert.ok(
      notes().every((n) => n.category !== null),
      'a note was left unsorted',
    );
    assert.ok(notes().some((n) => /release/i.test(n.text) && n.category === 'Stop'));
  });

  it('records the step-away and the self-correction', () => {
    assert.ok(session.events.some((e) => e.type === 'Deferred'));
    assert.ok(session.events.some((e) => e.type === 'CircledBack'));
    assert.ok(
      agentLines().some((l) => /Daniel.*circling back|circling back.*Daniel/i.test(l)),
      'the circle-back orchestration step was not spoken',
    );
    assert.ok(!notes().some((n) => /long tickets/i.test(n.text)), 'the corrected note was kept');
    assert.ok(notes().some((n) => /tickets detailed/i.test(n.text)));
  });

  it('records at least two action items with owners', () => {
    const actions = session.events.filter((e) => e.type === 'ActionItem' && !e.retracted);
    assert.ok(actions.length >= 2, `only ${actions.length} actions`);
    assert.ok(actions.every((a) => a.ownerMemberId));
  });

  it('creates a Scrum Master follow-up with CreateWorkItem changes that is still pending', () => {
    assert.ok(followUp, 'no follow-up was created');
    assert.equal(followUp.kind, 'RetroActions');
    assert.ok(followUp.forScrumMaster);
    assert.ok(followUp.changes.length >= 2);
    for (const change of followUp.changes) {
      assert.equal(change.op, 'CreateWorkItem');
      assert.equal(change.new?.type, 'Task');
      assert.ok(change.new?.tags.includes('retro-action'));
    }
    assert.notEqual(followUp.status, 'Applied');
  });
});
