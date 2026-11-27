# Decision and continuous evaluation

`decision-goldens.json` is 72 authored synthetic utterances in six categories,
not employee transcripts or an Azure DevOps export. Expected roster and sprint
ids belong only to `team-evaluation`; the tool never prepares sessions or writes
work items. Freeze its hash before a model run and review expectation changes
independently, never to match the latest model output.

```powershell
dotnet run --project tools\GateRunner -c Release -- --evaluate demo\evaluation\decision-goldens.json --out .m11-plan\run-log\evaluation-rules
$env:AZURE_TOKEN_CREDENTIALS = 'AzureCliCredential'
$env:EVALUATION_PROJECT_ENDPOINT = '<existing Foundry project endpoint>'
dotnet run --project tools\GateRunner -c Release -- --evaluate demo\evaluation\decision-goldens.json --model --release-check --out .m11-plan\run-log\evaluation-model
```

The real adapter emits pre-threshold probabilities; the same Domain grounding
checks roster and item ids. Rules have no probabilities or comment kinds.
Report exact intent/target/by, item-set and kind-set accuracy, dates with an
explicit eligible denominator, missing probabilities, failures, rejected
candidate ids, outside-id challenges and escaped ids. Brier/ECE use all binary
questions and the chosen-intent confidence, not invented rule probabilities.
At 0.7 and 0.6 report accepted precision and sample size; no denominator means
no passing evidence. Item candidates are constrained by the adapter itself, so
zero rejected candidates does not prove resistance to all fabricated ids.

G7 is release-blocking: all at least 60 cases need real, successful model
evidence; intent >=90%, item sets >=95%, comment kinds >=85%, spoken dates
>=95%, zero escaped ids, >=90% accepted precision at both thresholds and no
missing calibration answers. `--release-check` exits 1 when unmet or unrun.
CI is deliberately non-blocking and publishes failures. A daily synthetic
report is not live employee-trace quality evaluation. Enable real model CI
only with the existing approved OIDC identity and project endpoint variables;
no new identity/role is automatically created.

`foundry-continuous-evaluation.json` defines the rubric and the explicit disabled
state of employee-trace evaluation. Foundry requires input/output messages for
quality scores, but deployed content capture must remain off. Missing content
produces absent scores, not a valid quality pass. Knowledge and Work IQ
groundedness remain unrun because their sources are not built. ADR 0028 and
`docs/DEPLOY.md` record the privacy decision and exact prerequisite.
