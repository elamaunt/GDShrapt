using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Parses RENAME_VERIFICATION_VERIFIED.txt and tracks verification status.
/// </summary>
public class RenameVerificationParser
{
    /// <summary>
    /// Verification status for a rename edit entry.
    /// </summary>
    public enum VerificationStatus
    {
        OK,
        FP,
        Skip,
        Unverified
    }

    /// <summary>
    /// A verified edit entry from the verification file.
    /// </summary>
    public record VerifiedEdit(
        string RenameCase,
        string FilePath,
        int Line,
        int Column,
        string OldText,
        string NewText,
        string Confidence,
        VerificationStatus Status);

    // Pattern: LINE:COL OldText -> NewText [Confidence] (optional reason) # STATUS
    // Example: 2:12 BaseEntity -> BaseEntity_renamed [Strict] # OK
    private static readonly Regex EditPattern = new(
        @"^\s+(\d+):(\d+)\s+(\S+)\s+->\s+(\S+)\s+\[(\w+)\](?:\s+\(.*?\))?(?:\s+#\s*(OK|FP|SKIP))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pattern for file path line under edits section
    // Example:   test_scripts/base_entity.gd:
    private static readonly Regex FilePathPattern = new(
        @"^\s{2}(\S+\.gd):$",
        RegexOptions.Compiled);

    // Pattern for rename case header
    // Example: RENAME: BaseEntity -> BaseEntity_renamed
    private static readonly Regex RenameCasePattern = new(
        @"^RENAME:\s+(\S+)\s+->\s+(\S+)$",
        RegexOptions.Compiled);

    private readonly Dictionary<string, VerifiedEdit> _verifiedEdits = new();

    /// <summary>
    /// All verified edits indexed by lookup key.
    /// </summary>
    public IReadOnlyDictionary<string, VerifiedEdit> VerifiedEdits => _verifiedEdits;

    /// <summary>
    /// Parses the verification file.
    /// </summary>
    public void ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        string? currentRenameCase = null;
        string? currentFile = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Skip empty lines, comments, separators
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("===") || line.StartsWith("---"))
                continue;

            // Check for rename case header
            var renameMatch = RenameCasePattern.Match(line);
            if (renameMatch.Success)
            {
                currentRenameCase = $"{renameMatch.Groups[1].Value}->{renameMatch.Groups[2].Value}";
                currentFile = null;
                continue;
            }

            // Skip metadata lines (Source:, Result:, Message:, section headers)
            if (line.StartsWith("Source:") || line.StartsWith("Result:") || line.StartsWith("Message:"))
                continue;
            if (line.StartsWith("Strict edits") || line.StartsWith("Potential edits"))
                continue;
            if (line.Contains("(no edits)"))
                continue;

            // Check for file path
            var fileMatch = FilePathPattern.Match(line);
            if (fileMatch.Success)
            {
                currentFile = fileMatch.Groups[1].Value;
                continue;
            }

            // Try to parse as edit entry
            if (currentRenameCase != null && currentFile != null)
            {
                var editMatch = EditPattern.Match(line);
                if (editMatch.Success)
                {
                    var lineNum = int.Parse(editMatch.Groups[1].Value);
                    var column = int.Parse(editMatch.Groups[2].Value);
                    var oldText = editMatch.Groups[3].Value;
                    var newText = editMatch.Groups[4].Value;
                    var confidence = editMatch.Groups[5].Value;
                    var statusStr = editMatch.Groups[6].Value;

                    var status = statusStr.ToUpperInvariant() switch
                    {
                        "OK" => VerificationStatus.OK,
                        "FP" => VerificationStatus.FP,
                        "SKIP" => VerificationStatus.Skip,
                        _ => VerificationStatus.Unverified
                    };

                    var entry = new VerifiedEdit(
                        currentRenameCase,
                        currentFile,
                        lineNum,
                        column,
                        oldText,
                        newText,
                        confidence,
                        status);

                    var key = CreateKey(currentRenameCase, currentFile, lineNum, column);
                    _verifiedEdits[key] = entry;
                }
            }
        }
    }

    /// <summary>
    /// Creates a lookup key for an edit.
    /// </summary>
    public static string CreateKey(string renameCase, string filePath, int line, int column)
    {
        return $"{renameCase}|{filePath}|{line}:{column}";
    }

    /// <summary>
    /// Creates a lookup key for an edit entry.
    /// </summary>
    public static string CreateKey(VerifiedEdit entry)
    {
        return CreateKey(entry.RenameCase, entry.FilePath, entry.Line, entry.Column);
    }
}
