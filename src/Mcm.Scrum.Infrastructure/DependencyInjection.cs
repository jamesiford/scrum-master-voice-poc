using Azure.Core;
using Azure.Identity;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Ado;
using Mcm.Scrum.Infrastructure.Agents;
using Mcm.Scrum.Infrastructure.Demo;
using Mcm.Scrum.Infrastructure.Http;
using Mcm.Scrum.Infrastructure.Persistence;
using Mcm.Scrum.Infrastructure.Teams;
using Mcm.Scrum.Infrastructure.Voice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure;

/// <summary>Registers the adapters that implement Application ports.</summary>
public static class DependencyInjection
{
    private const string AdoScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
    private const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

    /// <summary>Registers infrastructure services and binds options with <c>ValidateOnStart</c>.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddOptions<AzureOptions>(services, configuration, AzureOptions.SectionName);
        AddOptions<StoreOptions>(services, configuration, StoreOptions.SectionName);
        AddOptions<AdoOptions>(services, configuration, AdoOptions.SectionName);
        AddOptions<DemoOptions>(services, configuration, DemoOptions.SectionName);
        AddOptions<FoundryOptions>(services, configuration, FoundryOptions.SectionName);
        AddOptions<VoiceLiveOptions>(services, configuration, VoiceLiveOptions.SectionName);
        services.AddOptions<BridgeOptions>().Bind(configuration.GetSection(BridgeOptions.SectionName));
        AddOptions<BotOptions>(services, configuration, BotOptions.SectionName);
        AddOptions<TeamDefaultsOptions>(services, configuration, TeamDefaultsOptions.SectionName);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<TokenCredential>(sp => new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { TenantId = sp.GetRequiredService<IOptions<AzureOptions>>().Value.TenantId }));
        services.AddSingleton<IPromptStore, FilePromptStore>();
        services.AddSingleton<IRosterSource, SeedFileRosterSource>();

        AddCollectionStore(services, configuration);
        services.AddSingleton<ISessionRepository, JsonFileSessionRepository>();
        services.AddSingleton<ITeamRepository, JsonFileTeamRepository>();
        services.AddSingleton<IMemberRepository, JsonFileMemberRepository>();
        services.AddSingleton<IFollowUpRepository, JsonFileFollowUpRepository>();
        services.AddSingleton<IScheduleRepository, JsonFileScheduleRepository>();
        services.AddSingleton<IDemoReset, JsonFileDemoReset>();
        services.AddSingleton<OutboxTeamsMessenger>();
        services.AddSingleton<IConversationStore, JsonFileConversationStore>();
        services.AddSingleton<ITeamsMessenger, TeamsBotMessenger>();
        services.AddSingleton<ICardOutbox>(sp => sp.GetRequiredService<OutboxTeamsMessenger>());

        services.AddHttpClient<IMeetingBridge, HttpMeetingBridge>((sp, client) =>
        {
            var bridge = sp.GetRequiredService<IOptions<BridgeOptions>>().Value;
            client.BaseAddress = new Uri((bridge.BaseUrl ?? bridge.RoomBaseUrl).TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        AddAdoClient(services);
        AddFoundry(services);
        services.AddSingleton<IVoiceSessionFactory, VoiceLiveSessionFactory>();
        services.AddOptions<ModelComparisonOptions>().Bind(configuration.GetSection(ModelComparisonOptions.SectionName));
        services.AddSingleton<IModelCatalog, ConfigModelCatalog>();
        services.AddSingleton<IEmailSender, OutboxEmailSender>();
        return services;
    }

    private static void AddCollectionStore(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CosmosOptions>().Bind(configuration.GetSection(CosmosOptions.SectionName));
        if (!string.Equals(configuration["Store:Mode"], "Cosmos", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICollectionStore, FileCollectionStore>();
            return;
        }

        services.AddSingleton(sp =>
        {
            var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            var client = new Microsoft.Azure.Cosmos.CosmosClient(
                cosmos.Endpoint,
                sp.GetRequiredService<TokenCredential>(),
                new Microsoft.Azure.Cosmos.CosmosClientOptions { UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) });
            return client.GetContainer(cosmos.Database, cosmos.Container);
        });
        services.AddSingleton<ICollectionStore>(sp => new CosmosCollectionStore(sp.GetRequiredService<Microsoft.Azure.Cosmos.Container>()));
    }

    private static void AddOptions<T>(IServiceCollection services, IConfiguration configuration, string section)
        where T : class
    {
        services.AddOptions<T>().Bind(configuration.GetSection(section)).ValidateDataAnnotations().ValidateOnStart();
    }

    private static void AddAdoClient(IServiceCollection services)
    {
        AddAdoHttpClient<IWorkItemReader, AdoRestClient>(services);
        AddAdoHttpClient<IWorkItemWriter, AdoWorkItemWriter>(services);
    }

    private static void AddAdoHttpClient<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddHttpClient<TService, TImplementation>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<AdoOptions>>().Value.Org.TrimEnd('/') + "/"))
            .AddHttpMessageHandler(sp =>
            {
                var ado = sp.GetRequiredService<IOptions<AdoOptions>>().Value;
                return ado.AuthMode == "Pat"
                    ? new PatAuthHandler(ado.Pat ?? throw new InvalidOperationException("Ado:Pat is required when Ado:AuthMode is Pat."))
                    : new BearerTokenHandler(sp.GetRequiredService<TokenCredential>(), AdoScope);
            });
    }

    private static void AddFoundry(IServiceCollection services)
    {
        services.AddHttpClient<FoundryChat>((sp, client) =>
            {
                var foundry = sp.GetRequiredService<IOptions<FoundryOptions>>().Value;
                client.BaseAddress = foundry.IsConfigured
                    ? new Uri(new Uri(foundry.ProjectEndpoint).GetLeftPart(UriPartial.Authority) + "/")
                    : new Uri("https://localhost/");
                client.Timeout = TimeSpan.FromSeconds(foundry.TimeoutSeconds);
            })
            .AddHttpMessageHandler(sp => new BearerTokenHandler(sp.GetRequiredService<TokenCredential>(), CognitiveServicesScope));
        services.AddTransient<IBacklogRiskAgent, FoundryBacklogRiskAgent>();
        services.AddTransient<IReconcileAgent, FoundryReconcileAgent>();
        services.AddTransient<IReportingAgent, FoundryReportingAgent>();
        services.AddTransient<IDraftingAgent, FoundryDraftingAgent>();
        services.AddSingleton<IKpiHistorySource, Reports.FixtureKpiHistorySource>();
    }
}
