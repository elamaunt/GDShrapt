namespace GDShrapt.Semantics;

public enum GDNodePresenceStatus
{
    /// <summary>
    /// Node always present (from .tscn, no removal code detected).
    /// </summary>
    AlwaysPresent,

    /// <summary>
    /// Node conditionally present (behind if/match or dynamically added).
    /// </summary>
    ConditionallyPresent,

    /// <summary>
    /// Node may be removed (queue_free()/remove_child() detected).
    /// </summary>
    MayBeAbsent,

    /// <summary>
    /// Node presence unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Prediction about whether a node exists at runtime.
/// </summary>
public class GDNodePresencePrediction
{
    public GDNodePresenceStatus Status { get; init; }
    public GDTypeConfidence Confidence { get; init; }
    public string Reason { get; init; } = "";

    public static GDNodePresencePrediction AlwaysPresentFromScene { get; } = new()
    {
        Status = GDNodePresenceStatus.AlwaysPresent,
        Confidence = GDTypeConfidence.Certain,
        Reason = "Node defined in .tscn file"
    };

    public static GDNodePresencePrediction UnknownPath { get; } = new()
    {
        Status = GDNodePresenceStatus.Unknown,
        Confidence = GDTypeConfidence.Unknown,
        Reason = "Node path not found in scene"
    };
}
