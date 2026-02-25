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

    /// <summary>Found as Public API — no internal callers, but externally accessible (class has class_name).</summary>
    FPA,

    /// <summary>C# Singleton Interop — autoloaded member potentially reachable from C# code.</summary>
    CSI,

    /// <summary>Variable may be accessed via Dynamic Access — self is passed to external code.</summary>
    VDA,

    /// <summary>Member referenced from .tres resource file property.</summary>
    TRF,

    /// <summary>Variable only used as Property Write target on locally-constructed non-escaping object.</summary>
    VPW,

    /// <summary>Member annotated with @public_api — explicitly marked as public API.</summary>
    PAA,

    /// <summary>Member annotated with @dynamic_use — explicitly marked as dynamically used.</summary>
    DUA,

    /// <summary>Member suppressed by custom user annotation (--suppress-annotation).</summary>
    CUA,
}
