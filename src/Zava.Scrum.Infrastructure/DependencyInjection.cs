using Azure.Core;
using Azure.Identity;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Agents;
using Zava.Scrum.Infrastructure.Demo;
using Zava.Scrum.Infrastructure.Http;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Infrastructure.Teams;
using Zava.Scrum.Infrastructure.Voice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure;

/// <summary>Registers the adapters that implement Application ports.</summary>
public static class DependencyInjection
{
    private const string AdoScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
    private const string GraphScope = "https://graph.microsoft.com/.default";
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
        AddOptions<UtteranceAnalysisOptions>(services, configuration, UtteranceAnalysisOptions.SectionName);
        AddOptions<VoiceLiveOptions>(services, configuration, VoiceLiveOptions.SectionName);
        services.AddOptions<BridgeOptions>().Bind(configuration.GetSection(BridgeOptions.SectionName));
        AddOptions<BotOptions>(services, configuration, BotOptions.SectionName);
        AddOptions<TeamDefaultsOptions>(services, configuration, TeamDefaultsOptions.SectionName);
        AddOptions<FlagsOptions>(services, configuration, FlagsOptions.SectionName);
        AddOptions<RetroOptions>(services, configuration, RetroOptions.SectionName);
        services.AddSingleton<IRetroSettings>(sp => sp.GetRequiredService<IOptions<RetroOptions>>().Value);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<TokenCredential>(sp => new CachingTokenCredential(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { TenantId = sp.GetRequiredService<IOptions<AzureOptions>>().Value.TenantId })));
        services.AddSingleton<IPromptStore, FilePromptStore>();
        services.AddSingleton<IRosterSource, SeedFileRosterSource>();

        AddCollectionStore(services, configuration);
        services.AddSingleton<ISessionRepository, JsonFileSessionRepository>();
        services.AddSingleton<ITeamRepository, JsonFileTeamRepository>();
        services.AddSingleton<IMemberRepository, JsonFileMemberRepository>();
        services.AddSingleton<IFollowUpRepository, JsonFileFollowUpRepository>();
        services.AddSingleton<IPlanningPackRepository, JsonFilePlanningPackRepository>();
        services.AddSingleton<ISprintCalendarRepository, JsonFileSprintCalendarRepository>();
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
        AddEmail(services, configuration);
        AddCalendar(services, configuration);
        return services;
    }

    private static void AddCalendar(IServiceCollection services, IConfiguration configuration)
    {
        // Team:HasSprintReview is a team setting (Rajeev has not confirmed a review), so it is read from the Team section, not Calendar.
        services.AddOptions<CalendarOptions>()
            .Bind(configuration.GetSection(CalendarOptions.SectionName))
            .Configure(o => o.HasSprintReview = configuration.GetValue<bool>("Team:HasSprintReview"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ICalendarSettings>(sp => sp.GetRequiredService<IOptions<CalendarOptions>>().Value);
        services.AddHttpClient<ICalendarWriter, GraphCalendarWriter>(client => client.BaseAddress = new Uri("https://graph.microsoft.com/"))
            .AddHttpMessageHandler(sp => new BearerTokenHandler(CalendarCredential(sp), GraphScope));
    }

    // The az CLI and developer tokens cannot write calendars, so an app registration with Calendars.ReadWrite can be given as a client secret.
    private static TokenCredential CalendarCredential(IServiceProvider sp)
    {
        var calendar = sp.GetRequiredService<IOptions<CalendarOptions>>().Value;
        return string.IsNullOrWhiteSpace(calendar.ClientId) || string.IsNullOrWhiteSpace(calendar.ClientSecret)
            ? sp.GetRequiredService<TokenCredential>()
            : new ClientSecretCredential(sp.GetRequiredService<IOptions<AzureOptions>>().Value.TenantId, calendar.ClientId, calendar.ClientSecret);
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        AddOptions<EmailOptions>(services, configuration, EmailOptions.SectionName);
        services.AddSingleton<OutboxEmailSender>();
        if (!string.Equals(configuration["Email:Mode"], "Graph", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<OutboxEmailSender>());
            return;
        }

        services.AddHttpClient<GraphEmailSender>(client => client.BaseAddress = new Uri("https://graph.microsoft.com/"))
            .AddHttpMessageHandler(sp => new BearerTokenHandler(sp.GetRequiredService<TokenCredential>(), GraphScope));
        services.AddTransient<IEmailSender>(sp => sp.GetRequiredService<GraphEmailSender>());
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
        AddAdoHttpClient<IIterationReader, AdoRestClient>(services);
        AddAdoHttpClient<IBacklogReader, AdoRestClient>(services);
        AddAdoHttpClient<IWorkItemWriter, AdoWorkItemWriter>(services);
        AddAdoHttpClient<IDemoAdoReset, AdoDemoReset>(services);
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
        services.AddSingleton<IFoundryAgentFactory, FoundryAgentFactory>();
        services.AddSingleton<FoundryChat>();
        services.AddTransient<IBacklogRiskAgent, FoundryBacklogRiskAgent>();
        services.AddTransient<IReconcileAgent, FoundryReconcileAgent>();
        services.AddTransient<FoundryUtteranceAnalyser>();
        services.AddHttpClient<DecisionUtteranceAnalyser>((sp, client) =>
            {
                // The project endpoint's host also serves the resource-level decision route.
                if (Uri.TryCreate(sp.GetRequiredService<IOptions<FoundryOptions>>().Value.ProjectEndpoint, UriKind.Absolute, out var endpoint))
                {
                    client.BaseAddress = new Uri($"{endpoint.Scheme}://{endpoint.Host}/");
                }
            })
            .AddHttpMessageHandler(sp => new BearerTokenHandler(sp.GetRequiredService<TokenCredential>(), "https://cognitiveservices.azure.com/.default"));
        services.AddTransient<IUtteranceAnalyser>(sp => sp.GetRequiredService<IOptions<UtteranceAnalysisOptions>>().Value.UseDecisionEngine
            ? sp.GetRequiredService<DecisionUtteranceAnalyser>()
            : sp.GetRequiredService<FoundryUtteranceAnalyser>());
        services.AddTransient<IReportingAgent, FoundryReportingAgent>();
        services.AddTransient<IDraftingAgent, FoundryDraftingAgent>();
        services.AddSingleton<IKpiHistorySource, Reports.FixtureKpiHistorySource>();
    }
}
