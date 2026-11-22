using System.ComponentModel.DataAnnotations;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Voice;

/// <summary>Binds <c>Retro__*</c>: the retrospective run by voice (M9b). Implements the Application port so handlers never read configuration.</summary>
public sealed class RetroOptions : IRetroSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Retro";

    /// <summary>Gets or sets the timebox in minutes; the agent wraps up at the end of it.</summary>
    [Range(1, 90)]
    public int Minutes { get; set; } = 15;

    /// <summary>Gets or sets the format: <c>WentWell</c> ("went well, do better, missing") or <c>StartStopContinue</c>.</summary>
    public RetroFormat Format { get; set; } = RetroFormat.WentWell;
}
