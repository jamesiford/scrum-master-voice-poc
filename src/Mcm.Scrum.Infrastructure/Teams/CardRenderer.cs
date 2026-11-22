using System.Reflection;
using System.Text.Json.Nodes;
using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Infrastructure.Teams;

/// <summary>Renders a <see cref="TeamsMessage"/> into Adaptive Card JSON from the templates in <c>Teams/Cards</c> (SPEC 12).</summary>
internal static class CardRenderer
{
    public static string Render(TeamsMessage message)
    {
        var template = Load(message.Kind switch
        {
            CardKind.PreBrief => "prebrief",
            CardKind.FollowUp => "followup",
            CardKind.FollowUpResult => "followup-result",
            CardKind.Absent => "absent",
            CardKind.Escalation => "escalation",
            CardKind.Unblock => "absent",
            CardKind.PlanningPack => "planning-pack",
            CardKind.RetroActions => "retro-actions",
            CardKind.ReviewPack => "review-pack",
            CardKind.SprintCalendar => "sprint-calendar",
            _ => "recap",
        });
        var card = JsonNode.Parse(template)!;
        Substitute(card, new Dictionary<string, string>
        {
            ["${title}"] = message.Title,
            ["${text}"] = message.Text,
            ["${followUpId}"] = message.FollowUpId?.ToString() ?? string.Empty,
        });
        var facts = card["body"]!.AsArray().First(b => b?["id"]?.GetValue<string>() == "facts")!["facts"]!.AsArray();
        foreach (var fact in message.Facts)
        {
            facts.Add(new JsonObject { ["title"] = fact.Label, ["value"] = fact.Value });
        }

        return card.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private static string Load(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().First(n => n.EndsWith($".Cards.{name}.json", StringComparison.Ordinal));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        return reader.ReadToEnd();
    }

    private static void Substitute(JsonNode? node, Dictionary<string, string> values)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (obj[key] is JsonValue v && v.TryGetValue<string>(out var s) && values.TryGetValue(s, out var replacement))
                    {
                        obj[key] = replacement;
                    }
                    else
                    {
                        Substitute(obj[key], values);
                    }
                }

                break;
            case JsonArray arr:
                foreach (var item in arr)
                {
                    Substitute(item, values);
                }

                break;
        }
    }
}
