/** The fields needed to display a roster identity; display names never resolve an account. */
export interface SpeakerRosterEntry {
  id: string;
  key: string;
  displayName: string;
  entraObjectId: string | null;
}

/** A readable name and honest identity provenance for the current speaker. */
export interface SpeakerLabel {
  displayName: string;
  identity: string;
}

/** Resolves only a roster id/key, not a matching display name, and clearly marks unmapped speakers. */
export function speakerLabel(
  speakerId: string | null,
  roster: readonly SpeakerRosterEntry[],
): SpeakerLabel | null {
  if (!speakerId) return null;
  const member = roster.find((entry) => entry.id === speakerId || entry.key === speakerId);
  return member
    ? {
        displayName: member.displayName,
        identity: member.entraObjectId
          ? 'Entra roster identity'
          : 'Guest / synthetic cast, no Entra identity',
      }
    : { displayName: 'Unmapped guest', identity: 'No verified roster identity' };
}
