using System.Text.RegularExpressions;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Parses FLOW_NARROWING_VERIFIED.txt.
/// Format:
///   file_path.gd
///     method()
///       LINE:COL varName -> narrowedType (base: baseType) # OK
/// </summary>
public class FlowNarrowingVerificationParser
{
    public enum VerificationStatus { OK, FP, Skip, Unverified }

    public record VerifiedNarrowing(
        string FilePath,
        string MethodName,
        string KeyLine,
        VerificationStatus Status);

    // Matches: LINE:COL varName -> narrowedType (base: baseType)
    private static readonly Regex EntryPattern = new(
        @"^(\d+):(\d+)\s+(\S+)\s+->\s+(.+?)\s+\(base:\s+(.+?)\)(?:\s+#\s*(OK|FP|SKIP))?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches method header: methodName()
    private static readonly Regex MethodPattern = new(
        @"^(\S+)\(\)\s*$",
        RegexOptions.Compiled);

    private readonly Dictionary<string, VerifiedNarrowing> _entries = new();

    public void ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        string? currentFile = null;
        string? currentMethod = null;

        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith("#") || string.IsNullOrWhiteSpace(rawLine))
                continue;

            // No indent = file header
            if (!rawLine.StartsWith(" "))
            {
                currentFile = rawLine.Trim();
                currentMethod = null;
                continue;
            }

            // 2-space indent = method header
            if (rawLine.StartsWith("  ") && !rawLine.StartsWith("    "))
            {
                var methodMatch = MethodPattern.Match(rawLine.Trim());
                if (methodMatch.Success)
                {
                    currentMethod = methodMatch.Groups[1].Value;
                }
                continue;
            }

            // 4-space indent = narrowing entry
            if (rawLine.StartsWith("    ") && currentFile != null && currentMethod != null)
            {
                var entryMatch = EntryPattern.Match(rawLine.Trim());
                if (entryMatch.Success)
                {
                    var status = entryMatch.Groups[6].Value.ToUpperInvariant() switch
                    {
                        "OK" => VerificationStatus.OK,
                        "FP" => VerificationStatus.FP,
                        "SKIP" => VerificationStatus.Skip,
                        _ => VerificationStatus.Unverified
                    };

                    var keyLine = $"{entryMatch.Groups[1].Value}:{entryMatch.Groups[2].Value} {entryMatch.Groups[3].Value} -> {entryMatch.Groups[4].Value} (base: {entryMatch.Groups[5].Value})";
                    var key = CreateKey(currentFile, currentMethod, keyLine);

                    _entries[key] = new VerifiedNarrowing(currentFile, currentMethod, keyLine, status);
                }
            }
        }
    }

    public static string CreateKey(string filePath, string methodName, string keyLine)
    {
        return $"{filePath}|{methodName}|{keyLine}";
    }

    public Dictionary<string, VerifiedNarrowing> GetVerifiedLookup()
    {
        return new Dictionary<string, VerifiedNarrowing>(_entries);
    }
}
