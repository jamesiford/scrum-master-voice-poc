var builder = DistributedApplication.CreateBuilder(args);

// Fixed ports keep the demo links stable: the API start endpoint returns a room URL on the bridge port (Bridge__RoomBaseUrl).
var api = builder.AddProject<Projects.Mcm_Scrum_Api>("api")
    .WithHttpHealthCheck("/healthz");

builder.AddNodeApp("bridge", "../meeting-bridge", "src/index.ts")
    .WithRunScript("start")
    .WithHttpEndpoint(port: 5090, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WithEnvironment("AZURE_TENANT_ID", builder.Configuration["Azure:TenantId"] ?? string.Empty)
    .WithEnvironment("SPEECH_ENDPOINT", builder.Configuration["Speech:Endpoint"] ?? string.Empty);

builder.AddViteApp("portal", "../admin-portal")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
