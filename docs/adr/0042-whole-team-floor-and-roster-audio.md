# ADR 0042: whole-team floor recovery and roster-first unmixed audio

## Context and evidence

The shared floor already protected opening/recap from ordinary barge-in,
but release after pause/takeover skipped the opening or resumed a stale
holder listening turn during recap. Announcements were not protected:
sustained speech changed them into listening with no member and no way
to advance. The native gateway selected the loudest frame before verifying
identity. Its previous focused test explicitly proved that guest1000
suppressed simultaneous member500.

## Decision

Opening, recap and announcements are complete whole-team steps. Ordinary
speech, including sustained guest overlap, does not interrupt them.
Explicit human takeover/pause cancels; release replays the exact original
phase. Human barge-in during these phases enters explicit takeover, rather
than silently dropping the phase. Normal member-turn barge-in is unchanged.

Put unmixed-source selection in pure `FloorControl`: prefer an audible
verified floor holder (amplitude>=300, the existing voice threshold),
otherwise the strongest verified roster source. Guests remain observable
independent of selection but their PCM is never sent to a model. Identity
is still resolved from the authenticated team roster object id, never name
or loudness. This is routing, not a new privilege or approval channel.
Keep the existing guest cross-talk silence rule and all adapters.

## Consequences and measurements

Initial targeted floor44/44 and native gateway10/10 pass. Six table cases cover
opening/recap/announcement takeover and pause replay. Source selection covers
louder guest/teammate, silent holder and guest-only inputs. The actual
gateway overlap fixture now forwards quieter member500 and zero guest PCM,
where the old fixture forwarded neither overlapping stream. The existing
guest-only, cross-talk and cancel/truncate tests remain passing.

Baseline actual Direct3418/4559ms p50/p95,n10; questions3469/4559ms,
n8, model8/10 and zero talkovers. Final API-only3ddd40b/ACRds18 rollout is
ready on zavaw3 revision90 with exact configuration/environment/identity/
bridge-image parity and unchanged defaults. Final floor49/49 and native
gateway10/10 pass, including the separate delivery guard in ADR0043.

After all60 retrospectives and the final112-case model evaluation finish,
mandatory8/8/native readiness5/5 pass. Actual Direct3615/4453ms,n12;
questions3628/4453ms,n9, model10/12, talkovers0/transcriptpieces19,
genuine approverrevision79/completeaudit. P50+5.76% exceeds5%, while
p95 improves2.33%. G1 remains failed; this worse point is a defect, not an
accepted regression or promotion. Different windows/populations are not a
paired comparison or causal attribution to the correctness fixes. Owning
performance work must investigate without changing thresholds.

These are synthetic routing/state tests, not new real Teams acoustic,
speaker-recognition, guest-screen-share or audible interruption acceptance.
Native acceptance remains n=0. No SDK/media protocol, permissions, recording
policy, approval invariant or default changes. Added resources and fixed
hourly cost: none/USD0. All older latency/gate failures remain evidence.
