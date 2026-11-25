import json, pathlib, re, collections, sys

root = pathlib.Path(__file__).resolve().parent
# Items the seed flags; the agent should raise each with the member who owns it (Edition B script).
flagged = {
    "m-admin": [("story 1 / retry", r"story 1\b|retry story|payment retry"), ("task 14 overdue", r"task 14|load test|past its due")],
    "m-maya": [("bug 12 blocker", r"bug 12|gateway"), ("story 5 PCI", r"story 5|pci"), ("task 16 schema review", r"task 16|schema")],
    "m-daniel": [("story 3 summary page", r"story 3|summary page"), ("task 15 logging", r"task 15|structured logging"), ("task 19 reconciliation", r"task 19|reconciliation")],
}
history = re.compile(r"standups? ago|last standup|previous standup|yesterday you (said|told|mentioned)|you mentioned earlier", re.I)

def load(label):
    out = []
    for f in sorted((root / label).glob("*.json")):
        d = json.loads(f.read_text(encoding="utf-8")); out.append({k.lower(): v for k, v in d.items()})
    return out

def analyse(labels):
    runs = [r for l in labels for r in load(l)]
    rows = collections.Counter()
    n = len(runs)
    examples = collections.defaultdict(list)
    for r in runs:
        ex = r.get("exchanges") or []
        agent = [(e.get("Text") or e.get("text")) for e in ex if (e.get("MemberId") or e.get("memberId")) is None]
        # coverage: an item counts as raised when an agent line mentions it after that member first speaks
        speaker_seen = set()
        text_after = collections.defaultdict(list)
        for e in ex:
            if (e.get("MemberId") or e.get("memberId")):
                speaker_seen.add((e.get("MemberId") or e.get("memberId")))
            else:
                for m in speaker_seen:
                    text_after[m].append((e.get("Text") or e.get("text")))
        for m, items in flagged.items():
            for name, rx in items:
                rows["items_expected"] += 1
                if any(re.search(rx, t, re.I) for t in text_after[m]):
                    rows["items_raised"] += 1
        for t in agent:
            if history.search(t):
                rows["invented_history"] += 1
                if len(examples["invented_history"]) < 3:
                    examples["invented_history"].append(t.replace("\n", " ")[:220])
        # a line repeated back to back within one agent turn ("Thanks, Daniel." twice)
        for t in agent:
            sentences = [s.strip() for s in re.split(r"[\n.]+", t) if s.strip()]
            if len(sentences) != len(set(sentences)):
                rows["repeated_sentence"] += 1
        for a, b in zip(agent, agent[1:]):
            if a.strip() and a.strip() == b.strip():
                rows["repeated_turn"] += 1
        rows["agent_turns"] += len(agent)
        rows["completed_timed_turns"] += len(r["turns"])
        rows["human_segments"] += r["humansegments"]
    return n, rows, examples

for name, labels in [("gpt-realtime-2.1", ["rt21-rules", "rt21-model"]), ("gpt-realtime-2.1-mini", ["mini-rules", "mini-model"])]:
    n, rows, ex = analyse(labels)
    print(f"## {name}: {n} standups")
    for k in ["agent_turns", "completed_timed_turns", "items_expected", "items_raised", "invented_history", "repeated_sentence", "repeated_turn"]:
        print(f"  {k}: {rows[k]}")
    if rows["items_expected"]:
        print(f"  flagged items raised: {100*rows['items_raised']/rows['items_expected']:.0f}%")
    for e in ex["invented_history"]:
        print("  e.g.", e)

