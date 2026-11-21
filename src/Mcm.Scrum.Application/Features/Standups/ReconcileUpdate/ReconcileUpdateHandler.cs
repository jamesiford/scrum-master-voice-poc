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
        var utterance = string.IsNullOrWhiteSpace(request.Asked) ? request.Utterance : $"(The member is answering this question about the item it names: \"{request.Asked}\") {request.Utterance}";
        var covered = request.Covered ?? [];
        var suggestion = await agent.ReconcileAsync(brief, utterance, ct);
        return Result<ReconcileResult>.Success(suggestion is null ? Fallback(brief, covered) : Ground(suggestion, brief, known, covered));
    }

    private static ReconcileResult Ground(ReconcileSuggestion s, MemberBrief brief, HashSet<WorkItemId> known, IReadOnlyCollection<int> covered)
    {
        var mentioned = s.MentionedWorkItemIds.Where(id => known.Contains(new WorkItemId(id))).Distinct().ToList();
        var skip = covered.ToHashSet();
        var question = !string.IsNullOrWhiteSpace(s.SuggestedQuestion) && WorkItemIdGuard.IsGrounded(s.SuggestedQuestion, known) && !AsksAbout(s.SuggestedQuestion, skip)
            ? s.SuggestedQuestion
            : FallbackQuestion(brief, skip);
        var events = s.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Text) && WorkItemIdGuard.IsGrounded(e.Text, known) && !IsNoBlockerStatement(e))
            .Select(e => new ReconcileEvent(e.Type, e.Text, e.WorkItemIds.Where(id => known.Contains(new WorkItemId(id))).Distinct().ToList()))
            .ToList();
        var risks = s.Risks.Where(r => WorkItemIdGuard.IsGrounded(r, known)).ToList();
        return new ReconcileResult(s.Consistent, mentioned, risks, question, events);
    }

    private static ReconcileResult Fallback(MemberBrief brief, IReadOnlyCollection<int> covered) =>
        new(true, [], [], FallbackQuestion(brief, covered), []);

    // The model sometimes re-asks about an item already answered; code decides which items are still open.
    private static bool AsksAbout(string question, HashSet<int> skip) =>
        Digits().Matches(question).Any(m => skip.Contains(int.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)));

    [GeneratedRegex(@"\b\d{1,9}\b")]
    private static partial Regex Digits();

    // "No blockers" is not a blocker. Models sometimes record it anyway, so the rule is enforced in code, not only in the prompt.
    private static bool IsNoBlockerStatement(SuggestedEvent e) =>
        e.Type == StandupEventType.Blocker && NoBlocker().IsMatch(e.Text);

    [GeneratedRegex(@"\b(no|none|nothing|not any|zero)\b.{0,20}\bblock|\bunblocked\b|\bnot blocked\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoBlocker();

    // A member with nothing flagged gets no question: a clarifying question must be grounded in a real flag.
    // Flagged items the member did not mention are asked about; ones they covered are not asked twice.
    private static string? FallbackQuestion(MemberBrief brief, IReadOnlyCollection<int> skip)
    {
        var open = brief.Items.Where(i => !skip.Contains(i.Id.Value)).ToList();
        return open.Any(i => i.Flags.Count > 0) ? FallbackQuestions.For(open) : null;
    }
}
