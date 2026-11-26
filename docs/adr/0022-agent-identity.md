# 0022. Agent identity (5B)

Status: accepted; built and verified live in `zavaw3`. Prompt 5B.

## Context

Prompt 4 put a hosted agent (`zava-voice-relay`) on Foundry. The platform gives each hosted agent a blueprint and an agent identity. ADR 0015 S4 decided the agent identity never touches Azure DevOps (reads use the API's managed identity, writes use the approver's delegated token, ADR 0020).

## Decision

1. **Adopt the identity Foundry creates; make no parallel one.** Graph shows it as `#microsoft.graph.agentIdentity` (type `ServiceIdentity`), with the blueprint `f491a1a5-...` and tags naming the agent, project, account and resource group. The hosted agent's relay already authenticates to Voice Live as this identity through `DefaultAzureCredential` (roles Cognitive Services User and Azure AI User on the Foundry account, assigned by `agent-host.ps1`, least privilege).
2. **No secret anywhere.** The identity has no password or certificate credential (checked by `infra/hooks/agent-identity.ps1`, which fails the deployment if there is one), and an architecture test scans `src/Zava.Scrum.AgentHost` for client secrets, keys and connection strings.
3. **A named sponsor.** The identity has a sponsor (`admin@`, set by the platform when the agent was created; the hook adds the deployer if a future agent has none). Entra uses the sponsor for lifecycle and attestation.
4. **Conditional Access and attestation are documented, not applied by the deployment** (`docs/DEPLOY.md`): they are tenant policy and need a Conditional Access administrator (human gate). The policies named are: block agent identities flagged high risk, and block non-production agents from production resources. The sponsor attests quarterly.
5. **No user account for the agent.** Graph does not need a user object: the agent makes no mailbox or presence call (calendar and mail go through the API's managed identity, ADR 0013). A user account would be created only when a prompt gives the agent a mailbox or Teams presence.
6. **Sign-ins are visible.** `GET /beta/auditLogs/signIns` filtered on `servicePrincipalId` and the `servicePrincipal` event type returns the agent's sign-ins with `agent.agentType = agenticAppInstance` and `agent.parentAppId` = the blueprint. Queries are in `docs/DEMO.md`.

## Measured and verified (zavaw3, 26 November 2026)

`agent-identity.ps1`: identity type agentIdentity PASS; credentials none PASS; blueprint `f491a1a5-510c-48fe-9cdc-b53afad9e75f`; sponsor `admin@MngEnvMCAP763514.onmicrosoft.com`; five recent sign-ins with `agentType agenticAppInstance`, resource Azure Machine Learning Services.

## Differences and not verified

- The prompt says "where the platform accepts it": Azure DevOps does not (S4), so this identity is used for Foundry and Voice Live only.
- A first sign-in filter without the event type timed out (Graph 504); the filter with `signInEventTypes` works.
- Conditional Access policies were **not created or tested** (human gate); a block of a risky agent was not exercised.
- Teams presence and mailbox for the agent were not built (decision 5).
