# Architecture decision records

Code wins over documentation; AS-BUILT records implementation and
[M11 gates](../gates/M11-gates.md) record acceptance. "Built" does not
mean every live gate passed. Historical measurements remain in their ADRs.

| ADR | Decision |
| --- | --- |
| [0001](0001-domain-events-in-domain.md) | Domain events belong in Domain |
| [0002](0002-m1-deviations.md) | M1 seed and pre-brief deviations |
| [0003](0003-m2-deviations.md) | M2 voice and floor-control deviations |
| [0004](0004-m3-deviations.md) | M3 follow-up deviations |
| [0005](0005-m3b-m7-deviations.md) | Teams bridge, messaging and deployment deviations |
| [0006](0006-m4-m6-deviations.md) | Scheduling, reports and Cosmos deviations |
| [0007](0007-m8-stretch-board-audio.md) | Stretch features, shared board and headless audio |
| [0008](0008-goal-driven-orchestration.md) | Reactive goal-driven orchestration |
| [0009](0009-documentation-baseline.md) | Documentation precedence and naming debt |
| [0009b](0009b-agent-framework-and-opentelemetry.md) | Agent Framework and telemetry baseline |
| [0010](0010-ceremony-kinds-packs-and-carry-over.md) | Ceremony patterns and carry-over |
| [0011](0011-sprint-planning-pack.md) | Separate planning aggregate and approval |
| [0012](0012-retrospective-by-voice.md) | Reuse voice engine for retrospectives |
| [0013](0013-sprint-calendar.md) | Approval-gated sprint calendar |
| [0014](0014-decision-model-and-barge-in.md) | Guarded decision model and barge-in |
| [0015](0015-m11-platform-decisions.md) | Measured M11 platform choices |
| [0015a](0015a-greenfield-deployment-model.md) | Empty-environment Bicep/hooks |
| [0016](0016-end-of-standup-cancellation.md) | Publish end events despite call cancellation |
| [0017](0017-identity-and-correlation-model.md) | Verified approver identity and audit chain |
| [0018](0018-multi-team-model-and-isolation.md) | Team membership, routing and capacity |
| [0019](0019-realtime-model-and-silence.md) | Retain measured realtime/silence defaults |
| [0020](0020-delegated-azure-devops-writes.md) | Approver-OBO writes and Entra ACS |
| [0021](0021-hosted-agent-split-and-state.md) | Stateless hosted relay and two-agent canary |
| [0022](0022-agent-identity.md) | Hosted Agent ID, sponsor and scoped access |
| [0023](0023-teams-native-media-bot.md) | Non-recording Windows native media alternative |
| [0024](0024-board-share-and-meeting-events.md) | Native board and matched meeting events |
| [0025](0025-copilot-channel.md) | Five asker-OBO skills and package 1.1.0 |
| [0026](0026-foundry-iq-permission-spike-prerequisites.md) | Knowledge permission spike blocked, unbuilt |
| [0027](0027-work-iq-delegated-advisory-contract.md) | Work IQ contracts only; live integrations blocked |
| [0028](0028-observability-and-evaluation.md) | Content-free telemetry and failed date release gate |
| [0029](0029-final-documentation-and-greenfield-proof.md) | Final documentation and G8 acceptance boundary |
| [0030](0030-final-validation-and-live-model-variance.md) | Final validation and retained live model failures |
| [0031](0031-greenfield-recovery-and-idempotent-hooks.md) | Authorised eastus2 recovery, compatible image/purge parameters and idempotent hooks |
| [0032](0032-foundry-workspace-version-recovery.md) | Bounded service-directed hosted-version workspace conflict recovery |
| [0033](0033-foundry-iq-consent-redo-and-fixture-boundary.md) | Knowledge consent verified; real source and permission fixtures still required |
| [0034](0034-work-iq-live-discovery-and-source-fixtures.md) | Work IQ delegated metadata discovery succeeds; scoped content and billing fixtures remain required |
| [0035](0035-hosted-agent-teardown-lifecycle.md) | Guarded hosted data-plane lifecycle cleanup before authorised project teardown |
| [0036](0036-film-evidence-surfaces.md) | Team-authorised audit page, measured Live counters and Entra/guest participant names |
| [0037](0037-guarded-demo-reseed-publication.md) | Explicit demo opt-in, approved recoverable ADO baseline and atomic application publication |
| [0038](0038-greenfield-proof-westus3.md) | Fresh westus3 greenfield proof with protected-environment lifecycle guards |
