using System.Text.Json;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Infrastructure.Teams;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using System.Text.Json.Serialization;
using StorageProbe;

if (args.Length != 4 || args[0] is not ("export" or "load" or "verify"))
    throw new ArgumentException("Usage: StorageProbe export|load|verify endpoint database output");
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
var payloadOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
var charges = new ChargeHandler();
using var client = new CosmosClient(args[1], new AzureCliCredential(), new CosmosClientOptions
{
    UseSystemTextJsonSerializerWithOptions = options,
    ConnectionMode = ConnectionMode.Gateway,
    CustomHandlers = { charges },
});
Directory.CreateDirectory(args[3]);
var database = client.GetDatabase(args[2]);
if (args[0] == "verify")
{
    await MigrationProof.RunAsync(database, args[3], options);
    return;
}
if (args[0] == "export")
{
    var manifest = new List<object>();
    using var containers = database.GetContainerQueryIterator<ContainerProperties>();
    while (containers.HasMoreResults)
    {
        foreach (var properties in await containers.ReadNextAsync())
        {
            var rows = new List<JsonElement>();
            double ru = 0;
            using var feed = database.GetContainer(properties.Id).GetItemQueryIterator<JsonElement>("SELECT * FROM c");
            while (feed.HasMoreResults)
            {
                var page = await feed.ReadNextAsync();
                rows.AddRange(page);
                ru += page.RequestCharge;
            }
            var bytes = JsonSerializer.SerializeToUtf8Bytes(rows, options);
            await File.WriteAllBytesAsync(Path.Combine(args[3], properties.Id + ".json"), bytes);
            manifest.Add(new { container = properties.Id, count = rows.Count, bytes = bytes.Length, ru,
                sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)) });
        }
    }
    await File.WriteAllTextAsync(Path.Combine(args[3], "manifest.json"), JsonSerializer.Serialize(manifest, options));
    Console.WriteLine(JsonSerializer.Serialize(manifest, options));
    return;
}

var container = database.GetContainer("TeamRecords");
var store = new CosmosTeamRecordStore(container);
var publication = new CosmosDemoStateStore(database.GetContainer("collections"));
if ((await publication.ReadAsync(default)).RecordStorageVersion != 1)
    throw new InvalidOperationException("Deploy and complete startup migration before measuring composed repository reads.");
var records = new RecordCollectionStore(new CosmosCollectionStore(database.GetContainer("collections")), store, publication);
var team = "storage-load-" + Guid.NewGuid().ToString("N");
var owned = new List<string>();
var measurements = new List<object>();
var sizes = new List<int>();
var started = DateTimeOffset.UtcNow;
double writeRu = 0;
try
{
    for (var standup = 1; standup <= 300; standup++)
    {
        var session = Guid.NewGuid().ToString();
        var firstIds = new Dictionary<string, string>();
        foreach (var (collection, count) in new[] { ("cards.json", 5), ("followups.json", 3), ("bot-activities.json", 10) })
        {
            for (var member = 0; member < count; member++)
            {
                var key = session + ":" + member;
                var message = new TeamsMessage(CardKind.FollowUp, team, Guid.Parse(session), key,
                    "Simulated standup follow-up", new string('x', 3000), [new CardFact("Member", "Participant " + member)]);
                object payload = collection == "followups.json"
                    ? FollowUp.Create(Guid.NewGuid(), team, Guid.Parse(session), "participant-" + member, FollowUpKind.Followup,
                        new string('x', 3000), [new ProposedChange(1, ChangeOp.Comment, "Synthetic proposal, never applied", "Storage load")],
                        DateOnly.FromDateTime(started.UtcDateTime), started).ToSnapshot()
                    : collection == "cards.json"
                        ? new SentCard(Guid.NewGuid(), started, message, CardRenderer.Render(message))
                        : new { TeamId = team, SessionId = Guid.Parse(session), DedupeKey = key,
                            ConversationKey = "synthetic-chat-" + member, ActivityId = "synthetic-activity-" + member, CreatedAt = started };
                var record = RecordCollectionStore.Describe(collection, JsonSerializer.SerializeToElement(payload, payloadOptions), 0);
                sizes.Add(JsonSerializer.SerializeToUtf8Bytes(record, options).Length);
                var response = await container.CreateItemAsync(record, new PartitionKey(team));
                writeRu += response.RequestCharge;
                owned.Add(record.Id);
                if (member == 0) firstIds[collection] = record.Id;
                if (standup == 1 && await store.CreateAsync(record with { Status = "MustNotReplace" }, default))
                    throw new InvalidOperationException("Duplicate create replaced a record.");
            }
        }
        if (standup is not (1 or 100 or 300)) continue;
        foreach (var collection in new[] { "cards.json", "followups.json", "bot-activities.json" })
        {
            var pointCharges = new List<double>();
            var queryCharges = new List<double>();
            var repositoryCharges = new List<double>();
            for (var sample = 0; sample < 10; sample++)
            {
                var point = await container.ReadItemAsync<TeamRecord>(firstIds[collection], new PartitionKey(team));
                pointCharges.Add(point.RequestCharge);
                var query = new QueryDefinition("SELECT * FROM c WHERE c.collection = @collection AND c.sessionId = @session")
                    .WithParameter("@collection", collection).WithParameter("@session", session);
                using var feed = container.GetItemQueryIterator<TeamRecord>(query,
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(team) });
                double charge = 0;
                var count = 0;
                while (feed.HasMoreResults)
                {
                    var page = await feed.ReadNextAsync();
                    charge += page.RequestCharge;
                    count += page.Count;
                }
                var expected = collection == "cards.json" ? 5 : collection == "followups.json" ? 3 : 10;
                if (count != expected || (await store.QueryAsync(collection, team, session, null, null, default)).Count != expected)
                    throw new InvalidOperationException("Session query returned unrelated historical records.");
                queryCharges.Add(charge);
                var before = charges.Total;
                var own = await records.QueryAsync<JsonElement>(collection, team, Guid.Parse(session), null, null, default);
                if (own.Count != expected) throw new InvalidOperationException("Composed repository query returned unrelated history.");
                repositoryCharges.Add(charges.Total - before);
            }
            measurements.Add(new { standups = standup, collection, n = 10, pointRu = pointCharges.Average(),
                sessionQueryRu = queryCharges.Average(), repositoryQueryRu = repositoryCharges.Average(),
                pointMin = pointCharges.Min(), pointMax = pointCharges.Max() });
        }
    }
    var result = new { team, standups = 300, records = owned.Count, http413 = 0, maximumItemBytes = sizes.Max(),
        totalItemBytes = sizes.Sum(), writeRu, elapsedSeconds = (DateTimeOffset.UtcNow - started).TotalSeconds, measurements };
    await File.WriteAllTextAsync(Path.Combine(args[3], "load.json"), JsonSerializer.Serialize(result, options));
    Console.WriteLine(JsonSerializer.Serialize(result, options));
}
finally
{
    foreach (var id in owned) await container.DeleteItemAsync<TeamRecord>(id, new PartitionKey(team));
    Console.WriteLine($"Removed {owned.Count} exclusively load-owned records from {team}.");
}
