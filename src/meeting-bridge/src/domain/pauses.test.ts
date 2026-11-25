import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { silence, splitPauses, stripPauses } from './pauses.js';

describe('splitPauses', () => {
  it('returns one segment for a line with no marker', () => {
    assert.deepEqual(splitPauses('All good.'), [{ text: 'All good.', pauseAfterMs: 0 }]);
  });

  it('puts the pause after the words that precede the marker', () => {
    assert.deepEqual(splitPauses('I finished the tests, [pause 700] and today I continue.'), [
      { text: 'I finished the tests,', pauseAfterMs: 700 },
      { text: 'and today I continue.', pauseAfterMs: 0 },
    ]);
  });

  it('handles several markers and ignores a marker with nothing before it', () => {
    assert.deepEqual(splitPauses('[pause 300] One [pause 500] two [pause 900] three'), [
      { text: 'One', pauseAfterMs: 500 },
      { text: 'two', pauseAfterMs: 900 },
      { text: 'three', pauseAfterMs: 0 },
    ]);
  });
});

describe('stripPauses', () => {
  it('leaves the words with single spaces', () => {
    assert.equal(
      stripPauses('Yesterday I shipped it, [pause 700] today I test.'),
      'Yesterday I shipped it, today I test.',
    );
  });
});

describe('silence', () => {
  it('is 48 bytes per millisecond of PCM16 24 kHz mono', () => {
    assert.equal(silence(500).length, 24000);
  });
});
