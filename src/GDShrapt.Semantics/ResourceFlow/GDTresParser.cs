using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics;

internal static class GDTresParser
{
    private static readonly Regex HeaderRegex = new(
        @"\[gd_resource\s+type=""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex ScriptClassRegex = new(
        @"\[gd_resource\s+[^\]]*script_class=""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex ExtResRegex = new(
        @"\[ext_resource\s+([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex PathRegex = new(
        @"path=""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex IdRegex = new(
        @"\sid=""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex TypeRegex = new(
        @"type=""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex ResourceSectionRegex = new(
        @"^\[resource\]\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex NextSectionRegex = new(
        @"^\[",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex PropertyRegex = new(
        @"^(\w+)\s*=\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ScriptExtResRegex = new(
        @"^\s*script\s*=\s*ExtResource\(\s*""?([^"")\s]+)""?\s*\)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex StringValueRegex = new(
        @"""([^""]+)""",
        RegexOptions.Compiled);

    public static string? ParseResourceType(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        var match = HeaderRegex.Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? ParseScriptClass(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        var match = ScriptClassRegex.Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? ParseScriptExtResourceId(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        var sectionContent = ExtractResourceSection(content);
        if (sectionContent == null)
            return null;

        var match = ScriptExtResRegex.Match(sectionContent);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static IReadOnlyList<GDTresExtResource> ParseExtResources(string content)
    {
        var result = new List<GDTresExtResource>();
        if (string.IsNullOrEmpty(content))
            return result;

        foreach (Match blockMatch in ExtResRegex.Matches(content))
        {
            var block = blockMatch.Value;
            var pathMatch = PathRegex.Match(block);
            var idMatch = IdRegex.Match(block);
            var typeMatch = TypeRegex.Match(block);

            if (pathMatch.Success && idMatch.Success)
            {
                result.Add(new GDTresExtResource
                {
                    Id = idMatch.Groups[1].Value,
                    Path = pathMatch.Groups[1].Value,
                    Type = typeMatch.Success ? typeMatch.Groups[1].Value : "Resource"
                });
            }
        }

        return result;
    }

    public static IReadOnlyList<GDTresProperty> ParseResourceProperties(string content)
    {
        var result = new List<GDTresProperty>();
        if (string.IsNullOrEmpty(content))
            return result;

        var sectionContent = ExtractResourceSection(content);
        if (sectionContent == null)
            return result;

        var lines = sectionContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Calculate line offset: find [resource] line number in original content
        var sectionMatch = ResourceSectionRegex.Match(content);
        int baseLineNumber = 1;
        if (sectionMatch.Success)
        {
            baseLineNumber = content.Substring(0, sectionMatch.Index).Split('\n').Length + 1;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var propMatch = PropertyRegex.Match(line);
            if (!propMatch.Success)
                continue;

            var name = propMatch.Groups[1].Value;
            var rawValue = propMatch.Groups[2].Value;

            // Skip script = ExtResource(...) — it's metadata, not a property
            if (name == "script")
                continue;

            var stringValues = new List<string>();
            foreach (Match strMatch in StringValueRegex.Matches(rawValue))
            {
                stringValues.Add(strMatch.Groups[1].Value);
            }

            result.Add(new GDTresProperty
            {
                Name = name,
                RawValue = rawValue,
                StringValues = stringValues,
                LineNumber = baseLineNumber + i
            });
        }

        return result;
    }

    public static GDTresParseResult ParseFull(string content)
    {
        return new GDTresParseResult
        {
            ResourceType = ParseResourceType(content),
            ScriptClass = ParseScriptClass(content),
            ScriptExtResourceId = ParseScriptExtResourceId(content),
            ExtResources = ParseExtResources(content),
            ResourceProperties = ParseResourceProperties(content)
        };
    }

    private static string? ExtractResourceSection(string content)
    {
        var sectionMatch = ResourceSectionRegex.Match(content);
        if (!sectionMatch.Success)
            return null;

        int start = sectionMatch.Index + sectionMatch.Length;

        // Find end: next section header or end of content
        var nextMatch = NextSectionRegex.Match(content, start);
        int end = nextMatch.Success ? nextMatch.Index : content.Length;

        return content.Substring(start, end - start);
    }
}

internal class GDTresExtResource
{
    public string Id { get; init; } = "";
    public string Path { get; init; } = "";
    public string Type { get; init; } = "Resource";
}

internal class GDTresProperty
{
    public string Name { get; init; } = "";
    public string RawValue { get; init; } = "";
    public IReadOnlyList<string> StringValues { get; init; } = Array.Empty<string>();
    public int LineNumber { get; init; }
}

internal class GDTresParseResult
{
    public string? ResourceType { get; init; }
    public string? ScriptClass { get; init; }
    public string? ScriptExtResourceId { get; init; }
    public IReadOnlyList<GDTresExtResource> ExtResources { get; init; } = Array.Empty<GDTresExtResource>();
    public IReadOnlyList<GDTresProperty> ResourceProperties { get; init; } = Array.Empty<GDTresProperty>();
}
