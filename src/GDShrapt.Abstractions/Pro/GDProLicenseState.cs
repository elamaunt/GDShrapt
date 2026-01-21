namespace GDShrapt.Abstractions;

/// <summary>
/// License states for Pro features.
/// Mirrors GDProLicenseStatus from Pro.Core for weak coupling.
/// </summary>
public enum GDProLicenseState
{
    /// <summary>
    /// Pro module is not linked (Base-only build).
    /// </summary>
    ProNotAvailable = -1,

    /// <summary>
    /// Subscription license is valid and active.
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Subscription license has expired.
    /// </summary>
    Expired = 1,

    /// <summary>
    /// License signature is invalid.
    /// </summary>
    Invalid = 2,

    /// <summary>
    /// Trial license (time-limited).
    /// </summary>
    Trial = 3,

    /// <summary>
    /// No license found.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Perpetual license valid for this release.
    /// </summary>
    Perpetual = 5,

    /// <summary>
    /// Perpetual license exists but this release is too new.
    /// </summary>
    UpdatesExpired = 6
}
