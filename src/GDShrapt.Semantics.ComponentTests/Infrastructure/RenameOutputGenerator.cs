using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Generates RENAME_VERIFICATION_OUTPUT.txt from rename plan results.
/// </summary>
public class RenameOutputGenerator
{
    /// <summary>
    /// Generates the output file with all rename plan results.
    /// </summary>
    public void GenerateOutput(
        List<(RenameTestCase TestCase, GDRenameResult Result)> results,
        string outputPath,
        string projectBasePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# RENAME_VERIFICATION_OUTPUT.txt");
        sb.AppendLine("# Auto-generated - do not edit manually");
        sb.AppendLine("# Format: LINE:COL OldName -> NewName [Confidence] (Reason)");
        sb.AppendLine();

        int totalCases = results.Count;
        int totalStrict = 0;
        int totalPotential = 0;

        foreach (var (tc, result) in results)
        {
            sb.AppendLine("=======================================");
            sb.AppendLine($"RENAME: {tc.OldName} -> {tc.NewName}");
            sb.AppendLine($"Source: {tc.SourceFile}");
            sb.AppendLine($"Result: {(result.Success ? "Success" : "Failed")}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                sb.AppendLine($"Message: {result.ErrorMessage}");

            sb.AppendLine("=======================================");
            sb.AppendLine();

            if (result.StrictEdits.Count > 0)
            {
                totalStrict += result.StrictEdits.Count;
                sb.AppendLine($"Strict edits ({result.StrictEdits.Count}):");
                WriteEditsGroupedByFile(sb, result.StrictEdits, projectBasePath);
                sb.AppendLine();
            }

            if (result.PotentialEdits.Count > 0)
            {
                totalPotential += result.PotentialEdits.Count;
                sb.AppendLine($"Potential edits ({result.PotentialEdits.Count}):");
                WriteEditsGroupedByFile(sb, result.PotentialEdits, projectBasePath);
                sb.AppendLine();
            }

            if (result.StrictEdits.Count == 0 && result.PotentialEdits.Count == 0)
            {
                sb.AppendLine("  (no edits)");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine($"# Summary: {totalCases} cases, {totalStrict} strict edits, {totalPotential} potential edits");

        File.WriteAllText(outputPath, sb.ToString());
    }

    private void WriteEditsGroupedByFile(StringBuilder sb, IReadOnlyList<GDTextEdit> edits, string projectBasePath)
    {
        var byFile = edits
            .GroupBy(e => e.FilePath)
            .OrderBy(g => GetRelativePath(g.Key, projectBasePath));

        foreach (var group in byFile)
        {
            var relativePath = GetRelativePath(group.Key, projectBasePath);
            sb.AppendLine($"  {relativePath}:");

            foreach (var edit in group.OrderBy(e => e.Line).ThenBy(e => e.Column))
            {
                var reason = !string.IsNullOrEmpty(edit.ConfidenceReason)
                    ? $" ({edit.ConfidenceReason})"
                    : "";
                sb.AppendLine($"    {edit.Line}:{edit.Column} {edit.OldText} -> {edit.NewText} [{edit.Confidence}]{reason}");
            }
        }
    }

    private string GetRelativePath(string? fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        fullPath = fullPath.Replace('\\', '/');
        basePath = basePath.Replace('\\', '/');
        if (!basePath.EndsWith("/"))
            basePath += "/";

        if (fullPath.StartsWith(basePath, System.StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(basePath.Length);
        }

        return fullPath;
    }
}

/// <summary>
/// Defines a single rename test case.
/// </summary>
public record RenameTestCase(string OldName, string NewName, string SourceFile);
