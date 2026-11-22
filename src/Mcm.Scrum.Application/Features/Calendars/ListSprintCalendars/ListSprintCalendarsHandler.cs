using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Calendars;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Calendars.ListSprintCalendars;

/// <summary>Reads the stored calendars.</summary>
internal sealed class ListSprintCalendarsHandler(ISprintCalendarRepository calendars) : IRequestHandler<ListSprintCalendarsQuery, Result<IReadOnlyList<SprintCalendarData>>>
{
    public async Task<Result<IReadOnlyList<SprintCalendarData>>> Handle(ListSprintCalendarsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<SprintCalendarData>>.Success([.. (await calendars.ListAsync(request.TeamId, ct)).Select(c => c.Data)]);
}
