using System.Text.RegularExpressions;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;

/// <summary>
/// Asks the model for phrasing and events, then drops every work item id and every question or event text that
/// mentions an id outside the sprint (rule 2). Without a model it falls back to a deterministic question.
/// </summary>
internal sealed partial class ReconcileUpdateHandler(ISessionRepository sessions, IReconcileAgent agent)
    : IRequestHandler<ReconcileUpdateQuery, Result<ReconcileResult>>
{
    public async Task<Result<ReconcileResult>> Handle(ReconcileUpdateQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<ReconcileResult>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var brief = session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == request.MemberId);
        if (brief is null)
        {
            return Result<ReconcileResult>.Failure(StandupErrors.MemberNotInBrief(request.MemberId));
        }

        var known = SprintIds.From(session.PreBrief);
        var knownIds = known.Select(k => k.Value).ToHashSet();
        var mentioned = ItemMentions.Detect(request.Utterance, brief.Items, knownIds).ToList();
        if (mentioned.Count == 0 && request.FocusItemId is { } focus && brief.Items.Any(i => i.Id.Value == focus))
        {
            // The member answered the question about this item without naming it.
            mentioned.Add(focus);
        }

        var skip = (request.Covered ?? []).Concat(mentioned).ToHashSet();
        var next = FallbackQuestions.MostUrgent([.. brief.Items.Where(i => !skip.Contains(i.Id.Value))]);
        var question = next is null ? null : FallbackQuestions.ForItem(next);

        var utterance = request.FocusItemId is null || string.IsNullOrWhiteSpace(request.Asked) ? request.Utterance : $"(The member is answering this question: \"{request.Asked}\") {request.Utterance}";
        var recorded = request.Correction ? RecordedFor(session, request.MemberId) : null;
        var suggestion = await agent.ReconcileAsync(brief, utterance, ct, recorded?.Select(r => r.Statement).ToList());
        var events = suggestion is null ? [] : GroundEvents(suggestion, brief, known, knownIds, mentioned);
        var risks = suggestion?.Risks.Where(r => WorkItemIdGuard.IsGrounded(r, known)).ToList() ?? [];
        return Result<ReconcileResult>.Success(new ReconcileResult(suggestion?.Consistent ?? true, mentioned, risks, question, events, next?.Id.Value, Retractions(recorded, suggestion)));
    }

    // Only this member's own recent statements can be retracted, and only by number, so a model cannot retract anyone else's.
    private static List<(RecordedStatement Statement, Guid Id)>? RecordedFor(StandupSession session, string memberId) =>
        session.ActiveEvents
            .Where(e => e.MemberId == memberId && e.Type is StandupEventType.Commitment or StandupEventType.Blocker or StandupEventType.Decision)
            .TakeLast(8)
            .Select((e, i) => (new RecordedStatement(i + 1, e.Type, e.Text), e.Id))
            .ToList();

    // Without a model the most recent statement is assumed to be the one corrected.
    private static List<Guid> Retractions(List<(RecordedStatement Statement, Guid Id)>? recorded, ReconcileSuggestion? suggestion)
    {
        if (recorded is null or { Count: 0 })
        {
            return [];
        }

        if (suggestion is null)
        {
            return [recorded[^1].Id];
        }

        return (suggestion.Retracts ?? []).Select(n => recorded.FirstOrDefault(r => r.Statement.Ref == n)).Where(r => r.Id != Guid.Empty).Select(r => r.Id).Distinct().ToList();
    }

    // Item links come from code (the ids in the event text, else the single item the member was talking about), never from model guesses.
    private static List<ReconcileEvent> GroundEvents(ReconcileSuggestion s, MemberBrief brief, HashSet<WorkItemId> known, HashSet<int> knownIds, List<int> mentioned) =>
        s.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Text) && WorkItemIdGuard.IsGrounded(e.Text, known) && !IsNoBlockerStatement(e))
            .Select(e =>
            {
                var ids = ItemMentions.Detect(e.Text, brief.Items, knownIds);
                return new ReconcileEvent(e.Type, e.Text, ids.Count > 0 ? ids : mentioned.Count == 1 ? [.. mentioned] : []);
            })
            .ToList();
    // "No blockers" is not a blocker. Models sometimes record it anyway, so the rule is enforced in code, not only in the prompt.
    private static bool IsNoBlockerStatement(SuggestedEvent e) =>
        e.Type == StandupEventType.Blocker && NoBlocker().IsMatch(e.Text);

    [GeneratedRegex(@"\b(no|none|nothing|not any|zero)\b.{0,20}\bblock|\bunblocked\b|\bnot blocked\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoBlocker();
}
