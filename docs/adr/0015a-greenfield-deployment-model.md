# 0015a. Greenfield deployment model

Status: **accepted, 24 November 2026**. Proven by `azd up` in an empty resource group; the evidence is in `docs/gates/M11-gates.md` (G8).

## Context

M11 needs `azd up` to build the whole current solution from an empty subscription with no manual portal steps. The previous templates referenced resources that had been created by hand (the bot registration, the Foundry account, the Cosmos database) and a client secret passed in as a parameter. Application behaviour does not change in this step.

## Decision

Three layers, in this order of preference.

**Declarative (Bicep, `infra/`).** A subscription-scope `main.bicep` creates the resource group and calls five modules: `observability`, `identity`, `foundry`, `data`, `compute`. Created, not referenced: Log Analytics, Application Insights, one user-assigned managed identity, the Foundry account and project with four deployments (chat, `gpt-realtime-2.1`, `gpt-live-1`, `zava-decision` on Microsoft-Decision-1.1), the container registry, Cosmos DB (serverless, Entra-only, the `collections` container), Azure Communication Services, the Container Apps environment and both apps, the Azure Bot with its Teams channel, and every role assignment. Parameters are `environmentName`, `location`, `tenantId`, `adoOrgUrl`, `adminUpn` and optional sizing. No parameter is a secret.

**Scripted and idempotent (`infra/hooks/`, PowerShell).** Entra objects, Easy Auth, Azure DevOps, the Teams package, the Exchange grant, preflight and verify. Scripts find existing objects (by tag, name or id) before creating, and a second run was shown to create nothing.

**Human gate.** Only where the platform forces an interactive identity: the Teams catalogue upload and the Exchange RBAC grant (docs/DEPLOY.md).

### Choices and the reasons

1. **Microsoft Graph Bicep extension not used.** The extension has v1.0 types, but I could not confirm on Learn that it is GA and supported by azd, and the portal app needs a scope, a pre-authorised client, tenant-wide consent and a secret that must not appear in a template. Hooks cover all of that and are idempotent, so the extension buys nothing yet. Revisit when it is GA in azd.
2. **No bot app registration or secret.** The Azure Bot resource supports `UserAssignedMSI` and the Teams channel works with it (Learn); `Microsoft.Agents.Authentication.Msal` 1.9.205 has `UserManagedIdentity`. The bot's id is the managed identity's client id, so the Teams manifest `id` and `botId` are that id. This replaces the client secret and the app registration. Consequence: the Teams app of a new environment has a new id; an existing install of `38a0a184-...` is not upgraded by this template (ADR 0015 S1).
3. **Easy Auth uses a client secret.** Container Apps Easy Auth supports only a client secret, not a federated credential (Learn). The hook creates one credential on the portal app and stores it only as the Container Apps secret `portal-secret`; it is configured in a hook because a Bicep re-provision replaces the secrets list, so the hook re-checks it on every provision. `defaultAuthorizationPolicy.allowedApplications` includes the Azure CLI client; without it a valid bearer token was rejected with 403.
4. **Registry permission mode set explicitly** to `LegacyRegistryPermissions`, with AcrPull for the apps identity and the Foundry project identity (ADR 0015 S3).
5. **Azure DevOps identity is the managed identity**, not an Entra agent identity (ADR 0015 S4, `VS403283`). The hook adds it with a Basic entitlement and to the project's Contributors. The entitlement POST is idempotent; the list endpoint does not support GET.
6. **Bridge key generated** at deployment (`newGuid()` as a `@secure()` default) and held as a Container Apps secret.
7. **Realtime quota.** Global `gpt-realtime-2.1` and `gpt-live-1` quota is subscription-wide, and the original environment used all of it. The new environment defaults to `DataZoneStandard` in `eastus2`. Preflight reads the catalogue and quota and says so before provisioning.
8. **Exchange RBAC for Applications** replaces the application access policy and the broad Graph application permissions: the identity holds Calendars.ReadWrite and Mail.Send on the organiser's mailbox only.

## Measured

See G8 in `docs/gates/M11-gates.md` for logs and times. Observations that shaped the design: the Container Apps environment took 13.5 minutes of a 21-minute provision; `zava-decision` at capacity 10 hit its 10 requests per minute limit, so the default is 100; `what-if` cannot expand nested modules that depend on earlier outputs.

## Consequences

- The old environment (`mcm-scrum-demo`) keeps its names and is not managed by these templates.
- Two human gates remain, and the portal app and Azure DevOps project survive `azd down` by design.
- A new-subscription run was not made; one subscription and tenant were available.
- The managed-identity bot path is configured and the endpoint rejects unauthenticated calls, but a Teams message round trip needs the app uploaded (a human gate) and was not run.
