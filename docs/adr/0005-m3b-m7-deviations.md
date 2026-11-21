# 0005. M3b and M7 decisions: Teams bot, Azure deployment

## Context
The Teams bot, proactive cards and an Azure deployment were requested after M3. The bot registration, dev tunnel and Azure resources were created in the demo subscription.

## Decision
- **Bot:** `ScrumAgent` (Microsoft 365 Agents SDK 1.9) in `Mcm.Scrum.Api/Bot` stores the conversation reference and turns the `approve` and `reject` card actions into the same commands the portal uses. The approver is the signed-in Teams user (`Activity.From.Name`), so nobody can approve as someone else. The card has no Edit action yet.
- **Proactive messaging:** `TeamsBotMessenger` wraps the outbox, sends the card to every stored conversation, remembers the activity id, and updates the card in place. `UpdateFollowUpCardHandler` replaces the card with its outcome after approve, reject or apply. DEMO: cards go to every stored conversation; PROD: route by Entra object id.
- **Inbound auth:** a JwtBearer validator accepts Bot Service and tenant tokens for audience = bot app id. `Bot:AppId` unset turns the bot off.
- **Dev tunnel guard:** the tunnel is anonymous and public, so when `Bot:TunnelHostSuffix` is set any request that arrives through it (host suffix or forwarding headers) gets 404 unless it is `/api/messages`. The portal API stays local.
- **Azure:** `azd up` deploys `api` (serving the portal from wwwroot) and `bridge` to Container Apps in the existing resource group, reusing the Foundry resource, Log Analytics and Application Insights. One user-assigned identity holds AcrPull, Cognitive Services User and Azure AI User. Container Apps sign-in (Entra) protects both apps; `/api/messages` and `/healthz` are excluded because the bot validates its own token. The post-deploy hook sets redirect URIs, points the bot at the API, and adds the identity to the Azure DevOps organisation and the project Contributors group.
- **Not deployed:** Cosmos, Azure Communication Services and the Chromium Teams-joining bridge do not exist yet. The JSON store lives on the container disk and is lost on restart, so the API runs one replica. PROD: Cosmos.

## Consequences
- Sideloading the Teams app is manual (the Graph catalog API needs a scope the CLI token lacks): upload `appPackage/mcm-scrum-agent.zip`.
- `/ws/bridge/{id}` sits behind Container Apps sign-in because Easy Auth excludes exact paths only. The Simulated Room therefore needs a browser session on the API domain; verify it interactively.