using Azure.Core;
using Azure.Identity;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Agents;
using Zava.Scrum.Infrastructure.Demo;
using Zava.Scrum.Infrastructure.Http;
using Zava.Scrum.Infrastructure.Identity;
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
        services.AddOptions<BridgeOptions>().Bind(configuration.GetSection(BridgeOptions.SectionName))
            .Validate(o => o.Mode is "Acs" or "Simulated" or "GraphMedia", "Bridge:Mode must be Acs, Simulated or GraphMedia.")
            .Validate(o => o.Mode != "GraphMedia" || Uri.TryCreate(o.GraphMediaBaseUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrWhiteSpace(o.ApiKey),
                "GraphMedia requires an HTTPS GraphMediaBaseUrl and a secret ApiKey.")
            .Validate(o => o.Mode != "GraphMedia" || !string.IsNullOrWhiteSpace(configuration["VoiceLive:RelayEndpoint"]),
                "GraphMedia requires the hosted VoiceLive:RelayEndpoint (ADR 0015 S6).")
            .ValidateOnStart();
        AddOptions<BotOptions>(services, configuration, BotOptions.SectionName);
        AddOptions<TeamDefaultsOptions>(services, configuration, TeamDefaultsOptions.SectionName);
        AddOptions<FlagsOptions>(services, configuration, FlagsOptions.SectionName);
        AddOptions<RetroOptions>(services, configuration, RetroOptions.SectionName);
        AddOptions<CapacityOptions>(services, configuration, CapacityOptions.SectionName);
        services.AddSingleton<ICapacitySettings>(sp => sp.GetRequiredService<IOptions<CapacityOptions>>().Value);
        services.AddSingleton<IRetroSettings>(sp => sp.GetRequiredService<IOptions<RetroOptions>>().Value);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<TokenCredential>(sp => new CachingTokenCredential(new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { TenantId = sp.GetRequiredService<IOptions<AzureOptions>>().Value.TenantId })));
        services.AddSingleton<IPromptStore, FilePromptStore>();
        services.AddSingleton<IRosterSource, SeedFileRosterSource>();
        AddIdentity(services, configuration);

        AddCollectionStore(services, configuration);
        // The voice route picks between two real adapters of the same port: the hosted agent (a relay endpoint is set) or Voice Live directly.
        if (!string.IsNullOrWhiteSpace(configuration["VoiceLive:RelayEndpoint"]))
        {
            services.AddHttpClient<IAgentHostWarmer, Hosting.HostedAgentWarmer>();
        }
        else
        {
            services.AddSingleton<IAgentHostWarmer, Hosting.NoHostWarmer>();
        }
        services.AddSingleton<ISessionRepository, SessionRepository>();
        services.AddSingleton<SessionMigration>();
        services.AddSingleton<ITeamRepository, JsonFileTeamRepository>();
        services.AddSingleton<IMemberRepository, JsonFileMemberRepository>();
        services.AddSingleton<ITeamMembershipRepository, JsonFileTeamMembershipRepository>();
        services.AddSingleton<IFollowUpRepository, JsonFileFollowUpRepository>();
        services.AddSingleton<IPlanningPackRepository, JsonFilePlanningPackRepository>();
        services.AddSingleton<ISprintCalendarRepository, JsonFileSprintCalendarRepository>();
        services.AddSingleton<IScheduleRepository, JsonFileScheduleRepository>();
        services.AddSingleton<IDemoReset, JsonFileDemoReset>();
        services.AddSingleton<OutboxTeamsMessenger>();
        services.AddSingleton<IConversationStore, JsonFileConversationStore>();
        services.AddSingleton<ITeamsMessenger, TeamsBotMessenger>();
        services.AddSingleton<ICardOutbox>(sp => sp.GetRequiredService<OutboxTeamsMessenger>());

        services.AddOptions<BoardOptions>().Bind(configuration.GetSection(BoardOptions.SectionName))
            .Validate(o => o.Mode is "Share" or "MediaShare", "Board:Mode must be Share or MediaShare.")
            .ValidateOnStart();
        services.AddHttpClient<HttpMeetingBridge>((sp, client) =>
        {
            var bridge = sp.GetRequiredService<IOptions<BridgeOptions>>().Value;
            client.BaseAddress = new Uri((bridge.BaseUrl ?? bridge.RoomBaseUrl).TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient<GraphMediaBridge>((sp, client) =>
        {
            var bridge = sp.GetRequiredService<IOptions<BridgeOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(bridge.GraphMediaBaseUrl))
            {
                client.BaseAddress = new Uri(bridge.GraphMediaBaseUrl.TrimEnd('/') + "/");
            }

            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddTransient<IMeetingBridge>(sp => sp.GetRequiredService<IOptions<BridgeOptions>>().Value.Mode == "GraphMedia"
            ? sp.GetRequiredService<GraphMediaBridge>() : sp.GetRequiredService<HttpMeetingBridge>());
        AddAdoClient(services);
        AddFoundry(services);
        services.AddSingleton<IVoiceSessionFactory, VoiceLiveSessionFactory>();
        services.AddOptions<ModelComparisonOptions>().Bind(configuration.GetSection(ModelComparisonOptions.SectionName));
        services.AddSingleton<IModelCatalog, ConfigModelCatalog>();
        AddEmail(services, configuration);
        AddCalendar(services, configuration);
        return services;
    }

    private static void AddIdentity(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>().Bind(configuration.GetSection(IdentityOptions.SectionName));
        services.AddSingleton<IIdentityPolicy, ConfigIdentityPolicy>();
        services.AddHttpClient<IIdentityDirectory, GraphIdentityDirectory>(client => client.BaseAddress = new Uri("https://graph.microsoft.com/"))
            .AddHttpMessageHandler(sp => new BearerTokenHandler(sp.GetRequiredService<TokenCredential>(), GraphScope));
    }

    private static void AddCalendar(IServiceCollection services, IConfiguration configuration)
    {
        // Team:HasSprintReview is a team setting (Rajeev has not confirmed a review), so it is read from the Team section, not Calendar.
        services.AddOptions<CalendarOptions>()
            .Bind(configuration.GetSection(CalendarOptions.SectionName))
            .Configure<IConfiguration>((o, configuration) => o.HasSprintReview = configuration.GetValue<bool>("Team:HasSprintReview"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ICalendarSettings>(sp => sp.GetRequiredService<IOptions<CalendarOptions>>().Value);
        services.AddHttpClient<IEntraGroupReader, GraphEntraGroupReader>(client => client.BaseAddress = new Uri("https://graph.microsoft.com/"))
            .AddHttpMessageHandler(sp => new BearerTokenHandler(sp.GetRequiredService<TokenCredential>(), GraphScope));
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
            services.AddSingleton<IApprovalRecordStore, FileApprovalRecordStore>();
            services.AddSingleton<ISessionItemStore, FileSessionItemStore>();
            return;
        }

        services.AddSingleton(sp => new Microsoft.Azure.Cosmos.CosmosClient(
            sp.GetRequiredService<IOptions<CosmosOptions>>().Value.Endpoint,
            sp.GetRequiredService<TokenCredential>(),
            new Microsoft.Azure.Cosmos.CosmosClientOptions { UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) }));
        services.AddSingleton<ICollectionStore>(sp =>
        {
            var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            return new CosmosCollectionStore(sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>().GetContainer(cosmos.Database, cosmos.Container));
        });
        services.AddSingleton<ISessionItemStore>(sp =>
        {
            var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            return new CosmosSessionItemStore(sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>().GetContainer(cosmos.Database, cosmos.SessionsContainer));
        });
        services.AddSingleton<IApprovalRecordStore>(sp =>
        {
            var cosmos = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            return new CosmosApprovalRecordStore(sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>().GetContainer(cosmos.Database, cosmos.ApprovalRecordsContainer));
        });
    }

    private static string? FirstSet(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static void AddOptions<T>(IServiceCollection services, IConfiguration configuration, string section)
        where T : class
    {
        services.AddOptions<T>().Bind(configuration.GetSection(section)).ValidateDataAnnotations().ValidateOnStart();
    }

    private static void AddAdoClient(IServiceCollection services)
    {
        // One client and credential per organisation, created on first use (ADR 0018). Nothing is bound to a single global organisation.
        services.AddHttpClient("ado-anonymous");
        services.AddSingleton<IAdoClientFactory, AdoClientFactory>();
        services.AddSingleton<RoutedWorkItemReader>();
        services.AddSingleton<IWorkItemReader>(sp => sp.GetRequiredService<RoutedWorkItemReader>());
        services.AddSingleton<IIterationReader>(sp => sp.GetRequiredService<RoutedWorkItemReader>());
        services.AddSingleton<IBacklogReader>(sp => sp.GetRequiredService<RoutedWorkItemReader>());
        services.AddSingleton<IWorkItemWriter, RoutedWorkItemWriter>();
        services.AddOptions<Delegation.DelegationOptions>().Configure<IConfiguration>((o, configuration) =>
        {
            configuration.GetSection(Delegation.DelegationOptions.SectionName).Bind(o);
            o.TenantId = FirstSet(o.TenantId, IdentityOptions.ResolveTenant(configuration));
            o.ClientId = FirstSet(o.ClientId, configuration["Portal:ClientId"]);
            o.ManagedIdentityClientId = FirstSet(o.ManagedIdentityClientId, configuration["AZURE_CLIENT_ID"]);
        });
        services.AddSingleton<IApproverTokenSource, Delegation.OboAdoTokenSource>();
        services.AddSingleton<IDemoAdoReset, RoutedDemoReset>();
        services.AddSingleton<IWorkItemScopeReader, AdoWorkItemScopeReader>();
        services.AddSingleton<IAdoConnectionTester, AdoConnectionTester>();
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
