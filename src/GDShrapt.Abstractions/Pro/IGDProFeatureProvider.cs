using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Interface for Pro feature provider.
/// Implemented by GDShrapt.Pro.Core when linked.
/// CLI.Core uses this to check Pro availability without direct dependency.
/// </summary>
public interface IGDProFeatureProvider
{
    /// <summary>
    /// Current license state.
    /// </summary>
    GDProLicenseState LicenseState { get; }

    /// <summary>
    /// Whether the provider has a valid license (any valid state).
    /// </summary>
    bool IsLicensed { get; }

    /// <summary>
    /// Checks if a specific Pro feature is enabled.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    /// <returns>True if the feature is enabled and licensed.</returns>
    bool IsFeatureEnabled(GDProFeatureKind feature);

    /// <summary>
    /// Gets the licensee name (for display purposes).
    /// </summary>
    string? LicenseeName { get; }

    /// <summary>
    /// Gets the license expiration date.
    /// </summary>
    DateTime? ExpiresAt { get; }

    /// <summary>
    /// Validates the license and returns the current state.
    /// </summary>
    GDProLicenseState ValidateLicense();
}
