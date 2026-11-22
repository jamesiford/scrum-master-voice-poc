using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Settings for a retrospective run by voice (<c>Retro:*</c>). A port so the handlers do not read configuration (SOLID: D).</summary>
public interface IRetroSettings
{
    /// <summary>Gets the timebox in minutes (<c>Retro:Minutes</c>, default 15).</summary>
    int Minutes { get; }

    /// <summary>Gets the format the agent asks for (<c>Retro:Format</c>, default <see cref="RetroFormat.WentWell"/>).</summary>
    RetroFormat Format { get; }
}
