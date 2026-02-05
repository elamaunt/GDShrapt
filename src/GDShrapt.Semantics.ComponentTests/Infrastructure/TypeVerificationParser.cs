using System.Text.RegularExpressions;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Parses TYPE_INFERENCE_VERIFIED.txt and tracks verification status.
/// </summary>
public class TypeVerificationParser
{
    /// <summary>
    /// Verification status for a type inference entry.
    /// </summary>
    public enum VerificationStatus
    {
        /// <summary>Type inference verified as correct.</summary>
        OK,
        /// <summary>False positive - type inference is wrong.</summary>
        FP,
        /// <summary>Skipped - known issue.</summary>
        Skip,
        /// <summary>Not verified yet.</summary>
        Unverified
    }

    /// <summary>
    /// A verified entry from the verification file.
    /// </summary>
    public record VerifiedEntry(
        string FilePath,
        int Line,
        int Column,
        string NodeKind,
        string Name,
        string Type,
        VerificationStatus Status);

    // Pattern: LINE:COL NODEKIND NAME -> TYPE # STATUS
    // Example: 30:8 Variable position -> Vector2 # OK
    private static readonly Regex EntryPattern = new(
        @"^(\d+):(\d+)\s+(\S+)\s+(.+?)\s+->\s+(.+?)(?:\s+#\s*(OK|FP|SKIP))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, List<VerifiedEntry>> _verifiedEntries = new();

    /// <summary>
    /// All verified entries indexed by file path.
    /// </summary>
    public IReadOnlyDictionary<string, List<VerifiedEntry>> VerifiedEntries => _verifiedEntries;

    /// <summary>
    /// Parses the verification file.
    /// </summary>
    public void ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        string? currentFile = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            // Check if this is a file header (no colon in position format)
            if (!line.Contains(":") || IsFilePath(line))
            {
                currentFile = line;
                if (!_verifiedEntries.ContainsKey(currentFile))
                    _verifiedEntries[currentFile] = new List<VerifiedEntry>();
                continue;
            }

            // Try to parse as entry
            if (currentFile != null)
            {
                var entry = ParseEntry(line, currentFile);
                if (entry != null)
                {
                    _verifiedEntries[currentFile].Add(entry);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a line is a file path (not an entry).
    /// </summary>
    private bool IsFilePath(string line)
    {
        // File paths contain / or \ and end with .gd
        return (line.Contains('/') || line.Contains('\\')) &&
               line.EndsWith(".gd", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a single entry line.
    /// </summary>
    private VerifiedEntry? ParseEntry(string line, string filePath)
    {
        var match = EntryPattern.Match(line);
        if (!match.Success)
            return null;

        var lineNum = int.Parse(match.Groups[1].Value);
        var column = int.Parse(match.Groups[2].Value);
        var nodeKind = match.Groups[3].Value;
        var name = match.Groups[4].Value.Trim();
        var type = match.Groups[5].Value.Trim();
        var statusStr = match.Groups[6].Value;

        var status = statusStr.ToUpperInvariant() switch
        {
            "OK" => VerificationStatus.OK,
            "FP" => VerificationStatus.FP,
            "SKIP" => VerificationStatus.Skip,
            _ => VerificationStatus.Unverified
        };

        return new VerifiedEntry(filePath, lineNum, column, nodeKind, name, type, status);
    }

    /// <summary>
    /// Creates a lookup key for an entry.
    /// </summary>
    public static string CreateKey(string filePath, int line, int column, string nodeKind)
    {
        return $"{filePath}|{line}:{column}|{nodeKind}";
    }

    /// <summary>
    /// Creates a lookup key for an entry.
    /// </summary>
    public static string CreateKey(VerifiedEntry entry)
    {
        return CreateKey(entry.FilePath, entry.Line, entry.Column, entry.NodeKind);
    }

    /// <summary>
    /// Gets all verified entries as a dictionary keyed by location.
    /// </summary>
    public Dictionary<string, VerifiedEntry> GetVerifiedLookup()
    {
        var lookup = new Dictionary<string, VerifiedEntry>();

        foreach (var kvp in _verifiedEntries)
        {
            foreach (var entry in kvp.Value)
            {
                var key = CreateKey(entry);
                lookup[key] = entry;
            }
        }

        return lookup;
    }
}
