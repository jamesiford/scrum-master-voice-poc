import assert from 'node:assert/strict';
import { after, before, describe, it } from 'node:test';
import { chromium, type Browser, type Page } from 'playwright-core';

/**
 * M2 demo check as an automated test: a scripted Simulated Room standup (SPEC 16, M2; SPEC 19).
 * The presenter's lines come from the demo cast and are streamed as microphone audio, so Azure Voice Live speech-to-text,
 * floor control, grounded questions, the parking lot, the absent member and the recap all run for real.
 *
 * Needs the stack running (`dotnet run --project src/Zava.Scrum.AppHost --launch-profile http`, or the API on :5080 and the
 * bridge on :5090), `az login`, a seeded sandbox (`tools/SeedAdo`) and Microsoft Edge. Run with `npm run e2e`.
 */

const api = process.env['E2E_API'] ?? 'http://localhost:5080';
const TEAM = 'team-payments';

interface SessionDetail {
  status: string;
  questionsAsked: number;
  p50LatencyMs: number;
  attendance: Record<string, string>;
  preBrief: {
    members: { items: { id: { value: number } }[] }[];
    unassignedItems: { id: { value: number } }[];
  };
  events: { type: string; workItemIds: number[]; text: string }[];
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

describe('Simulated Room standup (scenes 2 to 5)', () => {
  let browser: Browser;
  let page: Page;
  let captions: string[] = [];
  let session: SessionDetail;
  let latencyText = '';

  before(async () => {
    await post(`/api/teams/${TEAM}/members/sync`);
    const sessionId = await post<string>('/api/sessions/prepare', { teamId: TEAM });
    const start = await post<{ roomUrl: string }>(`/api/sessions/${sessionId}/start`, {
      mode: 'simulated',
      standupMinutes: 15,
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
    await page.getByRole('button', { name: 'Start standup' }).click();
    await page.waitForFunction(
      () => document.getElementById('floor')?.textContent?.startsWith('Closed'),
      null,
      { timeout: 8 * 60_000 },
    );

    captions = await page.locator('.caption').allInnerTexts();
    latencyText = await page.locator('#latency').innerText();
    session = (await (await fetch(`${api}/api/sessions/${sessionId}`)).json()) as SessionDetail;
  });

  after(async () => {
    await browser?.close();
  });

  const agentLines = () =>
    captions.filter((c) => c.startsWith('Scrum Agent')).map((c) => c.replace(/^Scrum Agent\n/, ''));

  it('opens by stating the sprint goal and calls every present member by first name', () => {
    assert.match(agentLines()[0] ?? '', /sprint goal/i);
    const text = agentLines().join(' ');
    for (const name of ['Daniel', 'Maya', 'Admin'])
      assert.ok(text.includes(name), `agent never addressed ${name}`);
  });

  it('asks at least two grounded questions', () => {
    assert.ok(session.questionsAsked >= 2, `asked ${session.questionsAsked}`);
  });

  it('never mentions a work item id outside the sprint', () => {
    const known = new Set(
      [
        ...session.preBrief.members.flatMap((m) => m.items),
        ...session.preBrief.unassignedItems,
      ].map((i) => i.id.value),
    );
    const mentioned = [
      ...agentLines()
        .join(' ')
        .matchAll(/(?:story|bug|task|item)\s+#?(\d+)/gi),
    ].map((m) => Number(m[1]));
    assert.deepEqual(
      mentioned.filter((id) => !known.has(id)),
      [],
    );
    assert.ok(session.events.every((e) => e.workItemIds.every((id) => known.has(id))));
  });

  it('parks the side discussion', async () => {
    assert.ok(session.events.some((e) => e.type === 'ParkingLot'));
    assert.ok((await page.locator('#parking li').count()) >= 1);
    assert.ok(agentLines().some((l) => /offline/i.test(l)));
  });

  it('marks nobody absent when everyone is in the room', () => {
    assert.ok(Object.values(session.attendance).every((a) => a !== 'Absent'));
  });

  it('closes with a recap that mentions follow-ups in Teams, and shows latency p50', () => {
    assert.match(agentLines().at(-1) ?? '', /Teams/);
    assert.equal(session.status, 'Ended');
    assert.match(latencyText, /p50 \d+ ms/);
    assert.ok(session.p50LatencyMs > 0);
  });
});
