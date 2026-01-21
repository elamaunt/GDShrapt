using System;
using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Tests.Pro;

/// <summary>
/// Mock implementation of IGDProFeatureProvider for testing.
/// </summary>
internal class MockProFeatureProvider : IGDProFeatureProvider
{
    public GDProLicenseState LicenseState { get; set; } = GDProLicenseState.Valid;
    public bool IsLicensed { get; set; } = true;
    public string? LicenseeName { get; set; } = "Test User";
    public DateTime? ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);

    private readonly HashSet<GDProFeatureKind> _enabledFeatures = new();
    private bool _allFeaturesEnabled = true;

    /// <summary>
    /// Creates a mock provider with all features enabled.
    /// </summary>
    public static MockProFeatureProvider Licensed()
    {
        return new MockProFeatureProvider
        {
            LicenseState = GDProLicenseState.Valid,
            IsLicensed = true
        };
    }

    /// <summary>
    /// Creates a mock provider with no valid license.
    /// </summary>
    public static MockProFeatureProvider Unlicensed()
    {
        return new MockProFeatureProvider
        {
            LicenseState = GDProLicenseState.NotFound,
            IsLicensed = false
        };
    }

    /// <summary>
    /// Creates a mock provider with expired license.
    /// </summary>
    public static MockProFeatureProvider Expired()
    {
        return new MockProFeatureProvider
        {
            LicenseState = GDProLicenseState.Expired,
            IsLicensed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    /// <summary>
    /// Creates a mock provider with trial license.
    /// </summary>
    public static MockProFeatureProvider Trial()
    {
        return new MockProFeatureProvider
        {
            LicenseState = GDProLicenseState.Trial,
            IsLicensed = true,
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };
    }

    /// <summary>
    /// Creates a mock provider with perpetual license.
    /// </summary>
    public static MockProFeatureProvider Perpetual()
    {
        return new MockProFeatureProvider
        {
            LicenseState = GDProLicenseState.Perpetual,
            IsLicensed = true,
            ExpiresAt = null
        };
    }

    /// <summary>
    /// Enables only specific features.
    /// </summary>
    public MockProFeatureProvider WithFeatures(params GDProFeatureKind[] features)
    {
        _allFeaturesEnabled = false;
        _enabledFeatures.Clear();
        foreach (var feature in features)
        {
            _enabledFeatures.Add(feature);
        }
        return this;
    }

    /// <summary>
    /// Disables all features by default, requiring explicit enabling.
    /// </summary>
    public MockProFeatureProvider WithNoFeatures()
    {
        _allFeaturesEnabled = false;
        _enabledFeatures.Clear();
        return this;
    }

    public bool IsFeatureEnabled(GDProFeatureKind feature)
    {
        if (!IsLicensed) return false;
        if (_allFeaturesEnabled) return true;
        return _enabledFeatures.Contains(feature);
    }

    public GDProLicenseState ValidateLicense()
    {
        return LicenseState;
    }
}
