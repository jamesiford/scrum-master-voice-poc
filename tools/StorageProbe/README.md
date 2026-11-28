# StorageProbe

Read-only Cosmos export and bounded synthetic per-item storage load using the
shipped `CosmosTeamRecordStore`. Uses Azure CLI credentials, never account keys.
Export files contain application data and must remain outside tracked source.

Run `dotnet run --project tools\StorageProbe -- export <endpoint> <database> <output>`
before migration. Run `load <endpoint> <database> <output>` after provisioning
`TeamRecords`; it creates 300 simulated standups in a unique synthetic team,
checks item size, first-write idempotency, indexed session queries and RU costs
at 1, 100 and 300 standups, then deletes only its own synthetic items.
Targeted adapter and real-mediator tests are in Infrastructure.Tests.

Run `verify <endpoint> <database> <export-directory>` after startup migration
to compare authoritative payloads, sessions, raw legacy sources and append-only
approval evidence with the pre-deployment export. A content-free proof is
written beside the unchanged export files.
