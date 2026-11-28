import assert from 'node:assert/strict';
import { test } from 'node:test';
import { speakerLabel } from './speakerLabel.ts';

const roster = [
  {
    id: 'member-admin',
    key: 'admin',
    displayName: 'Dana Admin',
    entraObjectId: 'verified-object-id',
  },
  { id: 'member-daniel', key: 'daniel', displayName: 'Daniel Park', entraObjectId: null },
];

test('speakerLabel resolves an Entra roster id or key to its readable name', () => {
  assert.deepEqual(speakerLabel('member-admin', roster), {
    displayName: 'Dana Admin',
    identity: 'Entra roster identity',
  });
  assert.equal(speakerLabel('admin', roster)?.displayName, 'Dana Admin');
});

test('speakerLabel labels unmapped cast and does not authenticate a matching display name', () => {
  assert.match(speakerLabel('daniel', roster)?.identity ?? '', /Guest \/ synthetic cast/);
  assert.equal(speakerLabel('Dana Admin', roster)?.displayName, 'Unmapped guest');
  assert.equal(speakerLabel(null, roster), null);
});
