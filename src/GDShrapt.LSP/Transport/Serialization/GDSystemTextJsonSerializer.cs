using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// System.Text.Json implementation of message serializer.
/// </summary>
public class GDSystemTextJsonSerializer : IGDMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public GDSystemTextJsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public GDSystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    public object? Deserialize(string json, Type type)
    {
        return JsonSerializer.Deserialize(json, type, _options);
    }
}
