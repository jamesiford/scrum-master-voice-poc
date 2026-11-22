using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Retrospectives.GetRetroBoard;

/// <summary>Reads what the retrospective has collected so far, for the shared board and the portal. A query: no side effects.</summary>
/// <param name="SessionId">The retrospective session.</param>
public sealed record GetRetroBoardQuery(Guid SessionId) : IQuery<RetroBoardDto>;

/// <summary>The retrospective board: three columns that fill while members speak, then the themes and actions.</summary>
/// <param name="Phase"><c>Round</c> while notes are collected, <c>Actions</c> once the themes were read back.</param>
/// <param name="Columns">The three columns, titled for the configured format.</param>
/// <param name="Themes">Named groups of notes.</param>
/// <param name="Actions">Agreed actions with owners.</param>
/// <param name="Unsorted">Notes still waiting for a category.</param>
public sealed record RetroBoardDto(string Phase, IReadOnlyList<RetroColumnDto> Columns, IReadOnlyList<RetroThemeView> Themes, IReadOnlyList<RetroActionView> Actions, int Unsorted);

/// <summary>One column of the board.</summary>
/// <param name="Category">Start, Stop or Continue.</param>
/// <param name="Label">The title for the configured format, for example "Went well".</param>
/// <param name="Notes">The notes in it.</param>
public sealed record RetroColumnDto(RetroCategory Category, string Label, IReadOnlyList<RetroNoteDto> Notes);

/// <summary>One note on the board.</summary>
/// <param name="Text">The note.</param>
/// <param name="Member">Who raised it.</param>
public sealed record RetroNoteDto(string Text, string Member);
