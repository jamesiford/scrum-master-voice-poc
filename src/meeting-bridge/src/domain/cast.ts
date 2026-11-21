/** One scripted line. A `when` line answers a question containing any of its keywords. */
export interface CastTurn {
  say: string;
  when?: string[];
}

/** A scripted participant (SPEC 7.3). */
export interface CastMember {
  memberId: string;
  voice: string;
  absent?: boolean;
  turns: CastTurn[];
}

/** A side conversation that starts right after a member's first line, to demonstrate the parking lot. */
export interface Sidebar {
  after: string;
  lines: { memberId: string; say: string }[];
}

/** The parsed cast file. */
export interface CastScript {
  members: CastMember[];
  sidebar?: Sidebar;
}

/** What a cast member says next, with the voice to use. */
export interface PlannedLine {
  memberId: string;
  voice: string;
  text: string;
}

/**
 * Chooses cast lines for one standup. Pure and stateful per session: the first time a member is asked
 * they give their first unconditional line; later they only speak when the agent's question matches a
 * `when` keyword, so an unrelated agent turn (for example a parking-lot interjection) gets no reply.
 */
export class CastPlanner {
  private readonly asked = new Map<string, number>();
  private readonly used = new Set<string>();
  private sidebarPlayed = false;

  /**
   * @param script - The parsed cast file.
   */
  constructor(private readonly script: CastScript) {}

  /**
   * Gets the next line for a member.
   * @param memberId - The member the agent called on.
   * @param question - The agent's most recent words, used to match `when` lines.
   * @returns The line, or `undefined` when the member stays quiet.
   */
  next(memberId: string, question: string): PlannedLine | undefined {
    const member = this.script.members.find((m) => m.memberId === memberId);
    if (!member || member.absent) return undefined;

    const count = this.asked.get(memberId) ?? 0;
    this.asked.set(memberId, count + 1);

    if (count === 0) {
      const first = member.turns.find((t) => !t.when);
      return first ? this.plan(member, first, 0) : undefined;
    }

    const lowered = question.toLowerCase();
    const index = member.turns.findIndex(
      (t, i) =>
        t.when &&
        !this.used.has(key(memberId, i)) &&
        t.when.some((w) => lowered.includes(w.toLowerCase())),
    );
    const turn = index >= 0 ? member.turns[index] : undefined;
    return turn ? this.plan(member, turn, index) : undefined;
  }

  /**
   * Gets the side conversation once, right after the trigger member's first line.
   * @param memberId - The member who just spoke.
   * @returns The sidebar lines, or an empty list.
   */
  takeSidebar(memberId: string): PlannedLine[] {
    const sidebar = this.script.sidebar;
    if (
      this.sidebarPlayed ||
      !sidebar ||
      sidebar.after !== memberId ||
      (this.asked.get(memberId) ?? 0) !== 1
    )
      return [];
    this.sidebarPlayed = true;
    return sidebar.lines.flatMap((l) => {
      const member = this.script.members.find((m) => m.memberId === l.memberId);
      return member ? [{ memberId: l.memberId, voice: member.voice, text: l.say }] : [];
    });
  }

  private plan(member: CastMember, turn: CastTurn, index: number): PlannedLine {
    this.used.add(key(member.memberId, index));
    return { memberId: member.memberId, voice: member.voice, text: turn.say };
  }
}

function key(memberId: string, index: number): string {
  return `${memberId}#${index}`;
}
