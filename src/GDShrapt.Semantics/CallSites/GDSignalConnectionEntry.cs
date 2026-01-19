using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a signal connection (emit -> callback).
/// Tracks where a signal is connected to a callback method.
/// </summary>
public class GDSignalConnectionEntry
{
    /// <summary>
    /// The file where the connection is made.
    /// </summary>
    public string SourceFilePath { get; }

    /// <summary>
    /// The method where connect() is called (null for scene connections).
    /// </summary>
    public string? SourceMethodName { get; }

    /// <summary>
    /// Line number of the connection.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column of the connection.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The type that emits the signal.
    /// </summary>
    public string? EmitterType { get; }

    /// <summary>
    /// The name of the signal.
    /// </summary>
    public string SignalName { get; }

    /// <summary>
    /// The class containing the callback method.
    /// </summary>
    public string? CallbackClassName { get; }

    /// <summary>
    /// The name of the callback method.
    /// </summary>
    public string CallbackMethodName { get; }

    /// <summary>
    /// Whether the signal name is dynamically determined (not a literal string).
    /// </summary>
    public bool IsDynamicSignal { get; }

    /// <summary>
    /// Whether the callback is dynamically determined.
    /// </summary>
    public bool IsDynamicCallback { get; }

    /// <summary>
    /// Whether this connection comes from a scene file (.tscn).
    /// </summary>
    public bool IsSceneConnection { get; }

    /// <summary>
    /// Confidence level of this connection.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Creates a new signal connection entry.
    /// </summary>
    public GDSignalConnectionEntry(
        string sourceFilePath,
        string? sourceMethodName,
        int line,
        int column,
        string? emitterType,
        string signalName,
        string? callbackClassName,
        string callbackMethodName,
        bool isDynamicSignal = false,
        bool isDynamicCallback = false,
        bool isSceneConnection = false,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict)
    {
        SourceFilePath = sourceFilePath;
        SourceMethodName = sourceMethodName;
        Line = line;
        Column = column;
        EmitterType = emitterType;
        SignalName = signalName;
        CallbackClassName = callbackClassName;
        CallbackMethodName = callbackMethodName;
        IsDynamicSignal = isDynamicSignal;
        IsDynamicCallback = isDynamicCallback;
        IsSceneConnection = isSceneConnection;
        Confidence = confidence;
    }

    /// <summary>
    /// Creates a signal connection from a scene file.
    /// </summary>
    public static GDSignalConnectionEntry FromScene(
        string sceneFilePath,
        int line,
        string emitterType,
        string signalName,
        string callbackClassName,
        string callbackMethodName)
    {
        return new GDSignalConnectionEntry(
            sceneFilePath,
            null,
            line,
            0,
            emitterType,
            signalName,
            callbackClassName,
            callbackMethodName,
            isDynamicSignal: false,
            isDynamicCallback: false,
            isSceneConnection: true,
            GDReferenceConfidence.Strict);
    }

    /// <summary>
    /// Creates a signal connection from code with known types.
    /// </summary>
    public static GDSignalConnectionEntry FromCode(
        string sourceFilePath,
        string sourceMethodName,
        int line,
        int column,
        string emitterType,
        string signalName,
        string callbackClassName,
        string callbackMethodName)
    {
        return new GDSignalConnectionEntry(
            sourceFilePath,
            sourceMethodName,
            line,
            column,
            emitterType,
            signalName,
            callbackClassName,
            callbackMethodName,
            isDynamicSignal: false,
            isDynamicCallback: false,
            isSceneConnection: false,
            GDReferenceConfidence.Strict);
    }

    /// <summary>
    /// Creates a signal connection with potential confidence (duck-typed).
    /// </summary>
    public static GDSignalConnectionEntry CreatePotential(
        string sourceFilePath,
        string sourceMethodName,
        int line,
        int column,
        string? emitterType,
        string signalName,
        string? callbackClassName,
        string callbackMethodName,
        bool isDynamicSignal = false,
        bool isDynamicCallback = false)
    {
        return new GDSignalConnectionEntry(
            sourceFilePath,
            sourceMethodName,
            line,
            column,
            emitterType,
            signalName,
            callbackClassName,
            callbackMethodName,
            isDynamicSignal,
            isDynamicCallback,
            isSceneConnection: false,
            GDReferenceConfidence.Potential);
    }

    public override string ToString()
    {
        var src = IsSceneConnection ? "scene" : "code";
        return $"[{src}] {EmitterType ?? "?"}.{SignalName} -> {CallbackClassName ?? "self"}.{CallbackMethodName}";
    }
}
