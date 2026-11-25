using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Identity;

/// <summary>
/// Reads the tenant and the administrator object ids from configuration. Fails closed: with no valid tenant configured, no identity
/// matches, so nothing can be approved.
/// </summary>
public sealed class ConfigIdentityPolicy : IIdentityPolicy
{
    private readonly HashSet<Guid> _admins;

    /// <summary>Initializes a new instance of the <see cref="ConfigIdentityPolicy"/> class.</summary>
    /// <param name="options">Identity options.</param>
    /// <param name="configuration">Configuration, for the tenant fallbacks.</param>
    public ConfigIdentityPolicy(IOptions<IdentityOptions> options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        var value = options.Value;
        var tenant = IdentityOptions.ResolveTenant(configuration);
        TenantId = Guid.TryParse(tenant, out var id) ? id : Guid.Empty;
        _admins = SplitAdmins(value.AdminObjectIds).Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToHashSet();
    }

    /// <inheritdoc />
    public Guid TenantId { get; }

    /// <inheritdoc />
    public bool IsAdmin(Guid objectId) => objectId != Guid.Empty && _admins.Contains(objectId);

    // The environment variable form is one comma-separated value.
    private static IEnumerable<string> SplitAdmins(string[] values) =>
        values.SelectMany(v => v.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
