using System.Text.Json;
using Microsoft.Agents.Core.Models;
using Zava.Scrum.Api.Bot;
using Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;
using Zava.Scrum.Domain.Reports;

namespace Zava.Scrum.Api.Tests;

public sealed class CopilotManifestTests
{
    [Fact]
    public void Manifest_CopilotUpgrade_KeepsBotIdentityScopesAndFiveExecutableStarters()
    {
        var root = FindRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "appPackage", "manifest.json")));
        var manifest = doc.RootElement;
        var bot = manifest.GetProperty("bots")[0];
        var agent = manifest.GetProperty("copilotAgents").GetProperty("customEngineAgents")[0];

        Assert.Equal("1.1.0", manifest.GetProperty("version").GetString());
        Assert.Equal("1.25", manifest.GetProperty("manifestVersion").GetString());
        Assert.Equal(bot.GetProperty("botId").GetString(), agent.GetProperty("id").GetString());
        Assert.Equal("bot", agent.GetProperty("type").GetString());
        var scopes = bot.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.Contains("personal", scopes);
        Assert.Contains("copilot", scopes);
        Assert.Contains("team", scopes);
        Assert.Contains("groupChat", scopes);
        var commands = bot.GetProperty("commandLists")[0].GetProperty("commands").EnumerateArray().ToList();
        Assert.Equal(5, commands.Count);
        Assert.All(commands, command => Assert.True(CopilotQuestions.TryParse(command.GetProperty("title").GetString(), out _, out _)));
        Assert.Equal(CopilotQuestions.Starters, commands.Select(c => c.GetProperty("title").GetString()));
        Assert.Contains(manifest.GetProperty("authorization").GetProperty("permissions").GetProperty("resourceSpecific").EnumerateArray(),
            permission => permission.GetProperty("name").GetString() == "OnlineMeeting.ReadBasic.Chat");
        Assert.False(manifest.TryGetProperty("staticTabs", out _));
    }

    [Theory]
    [InlineData("What is blocked on my team?", CopilotSkill.Blocked)]
    [InlineData("What did I commit to?", CopilotSkill.Commitments)]
    [InlineData("What is at risk in this sprint?", CopilotSkill.SprintRisk)]
    [InlineData("What is the standup status today?", CopilotSkill.StandupStatus)]
    [InlineData("Show my items and their state.", CopilotSkill.MyItems)]
    public void Question_MentionAndExplicitTeamPrefix_RoutesToPermissionScopedSkill(string text, CopilotSkill expected)
    {
        var activity = new Activity { Type = ActivityTypes.Message, Text = $"<at>Zava Scrum Agent</at> team t2: {text}", ChannelId = Channels.Msteams };

        Assert.True(CopilotMessage.IsSkillTurn(activity));
        Assert.True(CopilotQuestions.TryParse(CopilotMessage.Question(activity), out var skill, out var teamId));
        Assert.Equal(expected, skill);
        Assert.Equal("t2", teamId);
    }

    [Fact]
    public void Routing_UnknownCopilotQuestion_DoesNotFallThroughToLegacyServiceIdentityQa()
    {
        var activity = new Activity { Type = ActivityTypes.Message, Text = "Tell me everything", ChannelId = Channels.M365Copilot };

        Assert.True(CopilotMessage.IsSkillTurn(activity));
        Assert.False(CopilotQuestions.TryParse(activity.Text, out _, out _));
        activity.ChannelId = Channels.Msteams;
        Assert.False(CopilotMessage.IsSkillTurn(activity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void Card_ReadOnlyAnswer_OffersBoundedCodeGeneratedItemLinks(int count)
    {
        var link = "https://dev.azure.com/zava/p/_workitems/edit/1";
        var card = CopilotAnswerCard.Create(new("t1", "Facts", Enumerable.Repeat(link, count).ToList(), false));
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(card.Content));
        var actions = doc.RootElement.GetProperty("actions");
        var action = actions[0];

        Assert.Equal(Math.Min(count, 5), actions.GetArrayLength());
        Assert.Equal("Action.OpenUrl", action.GetProperty("type").GetString());
        Assert.Equal(link, action.GetProperty("url").GetString());
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "azure.yaml"))) { dir = dir.Parent; }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Cannot find repository root.");
    }
}
