using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.CLI.Core;

/// <summary>
/// JSON output formatter for machine-readable output.
/// </summary>
public class GDJsonFormatter : IGDOutputFormatter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string FormatName => "json";

    public void WriteAnalysisResult(TextWriter output, GDAnalysisResult result)
    {
        var json = JsonSerializer.Serialize(result, s_options);
        output.WriteLine(json);
    }

    public void WriteSymbols(TextWriter output, IEnumerable<GDSymbolInfo> symbols)
    {
        var json = JsonSerializer.Serialize(symbols, s_options);
        output.WriteLine(json);
    }

    public void WriteReferences(TextWriter output, IEnumerable<GDReferenceInfo> references)
    {
        var json = JsonSerializer.Serialize(references, s_options);
        output.WriteLine(json);
    }

    public void WriteReferenceGroups(TextWriter output, IEnumerable<GDReferenceGroupInfo> groups)
    {
        var json = JsonSerializer.Serialize(groups, s_options);
        output.WriteLine(json);
    }

    public void WriteFindRefsResult(TextWriter output, GDFindRefsResultInfo result)
    {
        var json = JsonSerializer.Serialize(result, s_options);
        output.WriteLine(json);
    }

    public void WriteListResult(TextWriter output, GDListResult result)
    {
        var json = JsonSerializer.Serialize(result, s_options);
        output.WriteLine(json);
    }

    public void WriteMessage(TextWriter output, string message)
    {
        var obj = new { message };
        var json = JsonSerializer.Serialize(obj, s_options);
        output.WriteLine(json);
    }

    public void WriteError(TextWriter output, string error)
    {
        var obj = new { error };
        var json = JsonSerializer.Serialize(obj, s_options);
        output.WriteLine(json);
    }
}
