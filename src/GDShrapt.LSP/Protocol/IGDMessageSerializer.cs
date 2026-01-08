using System;

namespace GDShrapt.LSP.Protocol;

/// <summary>
/// Abstraction for message serialization.
/// </summary>
public interface IGDMessageSerializer
{
    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    T? Deserialize<T>(string json);

    /// <summary>
    /// Deserializes a JSON string to an object of the specified type.
    /// </summary>
    object? Deserialize(string json, Type type);
}
