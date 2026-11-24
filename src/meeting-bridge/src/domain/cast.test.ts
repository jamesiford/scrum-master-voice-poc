import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { CastPlanner, type CastScript } from './cast.js';

const script: CastScript = {
  members: [
    {
      memberId: 'm-marcus',
      voice: 'v1',
      turns: [
        { say: 'I am blocked on bug 12.' },
        { when: ['platform', 'unblock'], say: 'The platform lead can help.' },
      ],
    },
    { memberId: 'm-tom', voice: 'v2', turns: [{ say: 'All good.' }] },
    { memberId: 'm-lena', voice: 'v3', absent: true, turns: [{ say: 'never' }] },
  ],
  sidebar: { after: 'm-marcus', lines: [{ memberId: 'm-tom', say: 'Side point.' }] },
};

describe('CastPlanner', () => {
  it('gives the first unconditional line on the first ask', () => {
    assert.equal(
      new CastPlanner(script).next('m-marcus', 'anything')?.text,
      'I am blocked on bug 12.',
    );
  });

  it('answers a later question only when a keyword matches, and only once', () => {
    const planner = new CastPlanner(script);
    planner.next('m-marcus', '');

    assert.equal(
      planner.next('m-marcus', 'Who on the Platform team can help?')?.text,
      'The platform lead can help.',
    );
    assert.equal(planner.next('m-marcus', 'Who on the platform team?'), undefined);
  });

  it('stays quiet when the agent turn is unrelated (for example a parking-lot interjection)', () => {
    const planner = new CastPlanner(script);
    planner.next('m-marcus', '');

    assert.equal(planner.next('m-marcus', "Let's take that offline."), undefined);
  });

  it('answers a question no keyword matches with its otherwise line, once', () => {
    const planner = new CastPlanner({
      members: [
        {
          memberId: 'm-tom',
          voice: 'v2',
          turns: [
            { say: 'All good.' },
            { when: ['risk'], say: 'The risk is retries.' },
            { otherwise: true, say: 'I will check after the standup.' },
          ],
        },
      ],
    });
    planner.next('m-tom', '');

    assert.equal(planner.next('m-tom', 'Still on track?')?.text, 'I will check after the standup.');
    assert.equal(planner.next('m-tom', 'What is the new date?'), undefined);
  });

  it('prefers a matching keyword line over the otherwise line', () => {
    const planner = new CastPlanner({
      members: [
        {
          memberId: 'm-tom',
          voice: 'v2',
          turns: [
            { say: 'All good.' },
            { otherwise: true, say: 'Fallback.' },
            { when: ['risk'], say: 'The risk is retries.' },
          ],
        },
      ],
    });
    planner.next('m-tom', '');

    assert.equal(planner.next('m-tom', 'Any risk?')?.text, 'The risk is retries.');
  });

  it('never speaks for an absent member or an unknown member', () => {
    const planner = new CastPlanner(script);

    assert.equal(planner.next('m-lena', ''), undefined);
    assert.equal(planner.next('m-nobody', ''), undefined);
  });

  it('plays the sidebar once, only right after the trigger member first line', () => {
    const planner = new CastPlanner(script);

    assert.deepEqual(planner.takeSidebar('m-marcus'), []);
    planner.next('m-marcus', '');
    assert.equal(planner.takeSidebar('m-tom').length, 0);
    assert.equal(planner.takeSidebar('m-marcus').length, 1);
    assert.equal(planner.takeSidebar('m-marcus').length, 0);
  });
});
