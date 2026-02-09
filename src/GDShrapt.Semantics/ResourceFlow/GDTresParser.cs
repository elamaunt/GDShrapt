using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics;

internal static class GDTresParser
{
    private static readonly Regex HeaderRegex = new(
        @"\[gd_resource\s+type=""([^""]+)""",
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

    public static string? ParseResourceType(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        var match = HeaderRegex.Match(content);
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
}

internal class GDTresExtResource
{
    public string Id { get; init; } = "";
    public string Path { get; init; } = "";
    public string Type { get; init; } = "Resource";
}
