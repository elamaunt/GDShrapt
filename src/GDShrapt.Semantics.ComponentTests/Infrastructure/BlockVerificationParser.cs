using System.Text.RegularExpressions;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Parses block-format verification files (TypeInfo, DuckTypes).
/// Blocks are multi-line entries where the first line is the key with optional # OK/FP/SKIP marker.
/// </summary>
public class BlockVerificationParser
{
    public enum VerificationStatus
    {
        OK,
        FP,
        Skip,
        Unverified
    }

    public record VerifiedBlock(
        string FilePath,
        string KeyLine,
        VerificationStatus Status,
        List<string> DetailLines);

    // Matches the status marker at the end of a key line: # OK, # FP, # SKIP
    private static readonly Regex StatusPattern = new(
        @"#\s*(OK|FP|SKIP)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, VerifiedBlock> _blocks = new();

    public void ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        string? currentFile = null;
        string? currentKeyLine = null;
        VerificationStatus currentStatus = VerificationStatus.Unverified;
        List<string>? currentDetails = null;

        foreach (var rawLine in lines)
        {
            // Skip global comments (lines starting with # at column 0)
            if (rawLine.StartsWith("#"))
                continue;

            // Empty line = block separator
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                FlushBlock(currentFile, currentKeyLine, currentStatus, currentDetails);
                currentKeyLine = null;
                currentDetails = null;
                continue;
            }

            // No indentation = file header
            if (!rawLine.StartsWith(" "))
            {
                FlushBlock(currentFile, currentKeyLine, currentStatus, currentDetails);
                currentKeyLine = null;
                currentDetails = null;
                currentFile = rawLine.Trim();
                continue;
            }

            // 2-space indent = key line (start of new block)
            if (rawLine.StartsWith("  ") && !rawLine.StartsWith("    "))
            {
                FlushBlock(currentFile, currentKeyLine, currentStatus, currentDetails);

                var line = rawLine.Trim();
                currentStatus = ExtractStatus(ref line);
                currentKeyLine = line;
                currentDetails = new List<string>();
                continue;
            }

            // 4+ space indent = detail line
            if (rawLine.StartsWith("    ") && currentDetails != null)
            {
                currentDetails.Add(rawLine.Trim());
            }
        }

        // Flush last block
        FlushBlock(currentFile, currentKeyLine, currentStatus, currentDetails);
    }

    private void FlushBlock(string? filePath, string? keyLine, VerificationStatus status, List<string>? details)
    {
        if (filePath == null || keyLine == null)
            return;

        var key = CreateKey(filePath, keyLine);
        _blocks[key] = new VerifiedBlock(filePath, keyLine, status, details ?? new List<string>());
    }

    private static VerificationStatus ExtractStatus(ref string line)
    {
        var match = StatusPattern.Match(line);
        if (!match.Success)
            return VerificationStatus.Unverified;

        var status = match.Groups[1].Value.ToUpperInvariant() switch
        {
            "OK" => VerificationStatus.OK,
            "FP" => VerificationStatus.FP,
            "SKIP" => VerificationStatus.Skip,
            _ => VerificationStatus.Unverified
        };

        // Remove the status marker from the key line
        line = line.Substring(0, match.Index).TrimEnd();
        return status;
    }

    public static string CreateKey(string filePath, string keyLine)
    {
        return $"{filePath}|{keyLine}";
    }

    public Dictionary<string, VerifiedBlock> GetVerifiedLookup()
    {
        return new Dictionary<string, VerifiedBlock>(_blocks);
    }
}
