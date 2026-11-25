/** A stretch of speech and the silence that follows it. */
export interface PausedSegment {
  /** What to say; never empty. */
  text: string;
  /** Silence after the segment, in milliseconds; 0 for the last one. */
  pauseAfterMs: number;
}

const marker = /\[pause (\d{1,5})\]/g;

/**
 * Splits a cast line on `[pause N]` markers so a line can hold a mid-sentence pause of N milliseconds. Used to test how long the voice
 * service waits before it decides a person has finished speaking. A line with no marker is one segment.
 * @param text - The scripted line.
 * @returns The segments in order.
 */
export function splitPauses(text: string): PausedSegment[] {
  const segments: PausedSegment[] = [];
  let last = 0;
  for (const m of text.matchAll(marker)) {
    const before = text.slice(last, m.index).trim();
    if (before) segments.push({ text: before, pauseAfterMs: Number(m[1]) });
    last = (m.index ?? 0) + m[0].length;
  }
  const tail = text.slice(last).trim();
  if (tail) segments.push({ text: tail, pauseAfterMs: 0 });
  // A leading marker with nothing before it has no segment to attach to; it is ignored rather than turned into silence.
  return segments;
}

/**
 * Removes the markers, leaving the words the room shows and the transcript expects.
 * @param text - The scripted line.
 * @returns The text with the markers removed and spaces collapsed.
 */
export function stripPauses(text: string): string {
  return text.replace(marker, ' ').replace(/\s+/g, ' ').trim();
}

/**
 * Builds silence as PCM16 24 kHz mono (2 bytes a sample, 24 samples a millisecond).
 * @param ms - Duration in milliseconds.
 * @returns A zero-filled buffer.
 */
export function silence(ms: number): Buffer {
  return Buffer.alloc(ms * 24 * 2);
}
