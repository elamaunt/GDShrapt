namespace GDShrapt.Abstractions;

/// <summary>
/// Compact reason codes for dead code detection results.
/// </summary>
public enum GDDeadCodeReasonCode
{
    /// <summary>Variable Never Read.</summary>
    VNR,

    /// <summary>Variable has @export (engine-visible, may be set from inspector/scene).</summary>
    VEX,

    /// <summary>Variable has @onready (engine-managed initialization).</summary>
    VOR,

    /// <summary>Function has No Callers.</summary>
    FNC,

    /// <summary>Function may be called via Duck-Typing.</summary>
    FDT,

    /// <summary>Signal Never Emitted (and has no connections).</summary>
    SNE,

    /// <summary>Signal Connected But never emitted.</summary>
    SCB,

    /// <summary>Constant Never Used.</summary>
    CNU,

    /// <summary>Parameter Never Used.</summary>
    PNU,

    /// <summary>Unreachable Code after return/break/continue.</summary>
    UCR,

    /// <summary>Enum value Never Used.</summary>
    ENU,

    /// <summary>Inner Class Unused.</summary>
    ICU,
}
