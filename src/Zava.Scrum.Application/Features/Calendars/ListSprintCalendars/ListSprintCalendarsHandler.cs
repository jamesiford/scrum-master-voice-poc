using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Calendars.ListSprintCalendars;

/// <summary>Reads the stored calendars.</summary>
internal sealed class ListSprintCalendarsHandler(ISprintCalendarRepository calendars) : IRequestHandler<ListSprintCalendarsQuery, Result<IReadOnlyList<SprintCalendarData>>>
{
    public async Task<Result<IReadOnlyList<SprintCalendarData>>> Handle(ListSprintCalendarsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<SprintCalendarData>>.Success([.. (await calendars.ListAsync(request.TeamId, ct)).Select(c => c.Data)]);
}
