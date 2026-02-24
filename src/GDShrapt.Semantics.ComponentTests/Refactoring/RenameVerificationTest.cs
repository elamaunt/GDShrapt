using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for rename planning.
/// Generates RENAME_VERIFICATION_OUTPUT.txt and compares with RENAME_VERIFICATION_VERIFIED.txt.
/// Test fails until all entries are verified with # OK marker.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class RenameVerificationTest
{
    private static string OutputPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "RENAME_VERIFICATION_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "RENAME_VERIFICATION_VERIFIED.txt");

    private static readonly RenameTestCase[] TestCases = new[]
    {
        new RenameTestCase("BaseEntity", "BaseEntity_renamed", "base_entity.gd"),
        new RenameTestCase("health_changed", "health_changed_renamed", "base_entity.gd"),
        new RenameTestCase("take_damage", "take_damage_renamed", "base_entity.gd"),
        new RenameTestCase("MAGIC_NUMBER", "MAGIC_NUMBER_RENAMED", "refactoring_targets.gd"),
        new RenameTestCase("armor", "armor_renamed", "player_entity.gd"),
        new RenameTestCase("counter", "counter_renamed", "rename_test.gd"),
        new RenameTestCase("current_health", "current_health_renamed", "base_entity.gd"),
        new RenameTestCase("calculate_score", "calculate_score_renamed", "refactoring_targets.gd"),
        new RenameTestCase("score_changed", "score_changed_renamed", "refactoring_targets.gd"),
        new RenameTestCase("player_speed", "player_speed_renamed", "refactoring_targets.gd"),
    };

    [TestMethod]
    public void AllRenames_MustMatchVerifiedOutput()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;
        var service = TestProjectFixture.ProjectModel.Services.Rename;

        // 1. Run all 10 rename plans
        var results = new List<(RenameTestCase TestCase, GDRenameResult Result)>();
        foreach (var tc in TestCases)
        {
            var script = TestProjectFixture.GetScript(tc.SourceFile);
            Assert.IsNotNull(script, $"Script not found: {tc.SourceFile}");

            GDRenameResult result;
            var symbol = script!.SemanticModel?.FindSymbol(tc.OldName);

            if (symbol != null)
            {
                result = service.PlanRename(symbol, tc.NewName);
            }
            else
            {
                result = service.PlanRename(tc.OldName, tc.NewName, script.FullPath);
            }

            results.Add((tc, result));
        }

        // 2. Generate output file
        var generator = new RenameOutputGenerator();
        generator.GenerateOutput(results, OutputPath, projectPath);

        // 3. Parse verified file
        var parser = new RenameVerificationParser();
        parser.ParseFile(VerifiedPath);
        var verifiedLookup = parser.VerifiedEdits;

        // 4. Compare and collect statistics
        var unverifiedEntries = new List<(string RenameCase, string File, GDTextEdit Edit)>();
        var falsePositives = new List<(string RenameCase, string File, GDTextEdit Edit, RenameVerificationParser.VerifiedEdit Verified)>();
        var duplicateEdits = new List<(string RenameCase, string File, GDTextEdit Edit)>();
        int totalEdits = 0;
        int verifiedOk = 0;
        int skipped = 0;
        var seenKeys = new HashSet<string>();

        foreach (var (tc, result) in results)
        {
            var renameCase = $"{tc.OldName}->{tc.NewName}";

            foreach (var edit in result.StrictEdits.Concat(result.PotentialEdits))
            {
                totalEdits++;
                var relativePath = GetRelativePath(edit.FilePath, projectPath);
                var key = RenameVerificationParser.CreateKey(renameCase, relativePath, edit.Line, edit.Column);

                if (!seenKeys.Add(key))
                {
                    duplicateEdits.Add((renameCase, relativePath, edit));
                    continue;
                }

                if (verifiedLookup.TryGetValue(key, out var verified))
                {
                    switch (verified.Status)
                    {
                        case RenameVerificationParser.VerificationStatus.OK:
                            verifiedOk++;
                            break;
                        case RenameVerificationParser.VerificationStatus.FP:
                            falsePositives.Add((renameCase, relativePath, edit, verified));
                            break;
                        case RenameVerificationParser.VerificationStatus.Skip:
                            skipped++;
                            break;
                        default:
                            unverifiedEntries.Add((renameCase, relativePath, edit));
                            break;
                    }
                }
                else
                {
                    unverifiedEntries.Add((renameCase, relativePath, edit));
                }
            }
        }

        // 5. Generate report
        var report = new StringBuilder();
        report.AppendLine("RENAME VERIFICATION REPORT");
        report.AppendLine("==========================");
        report.AppendLine($"Total edits:       {totalEdits}");
        report.AppendLine($"Verified (OK):     {verifiedOk}");
        report.AppendLine($"Skipped:           {skipped}");
        report.AppendLine($"Unverified:        {unverifiedEntries.Count}");
        report.AppendLine($"False Positives:   {falsePositives.Count}");
        report.AppendLine($"Duplicate edits:   {duplicateEdits.Count}");
        report.AppendLine();

        if (unverifiedEntries.Count > 0)
        {
            report.AppendLine("UNVERIFIED ENTRIES:");
            report.AppendLine("-------------------");

            string? lastCase = null;
            foreach (var (renameCase, file, edit) in unverifiedEntries)
            {
                if (renameCase != lastCase)
                {
                    report.AppendLine();
                    report.AppendLine($"  [{renameCase}]");
                    lastCase = renameCase;
                }
                var reason = !string.IsNullOrEmpty(edit.ConfidenceReason) ? $" ({edit.ConfidenceReason})" : "";
                report.AppendLine($"    {file}:{edit.Line}:{edit.Column} [{edit.Confidence}]{reason}");
            }
        }

        if (falsePositives.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("FALSE POSITIVES:");
            report.AppendLine("----------------");

            foreach (var (renameCase, file, edit, _) in falsePositives)
            {
                report.AppendLine($"  [{renameCase}] {file}:{edit.Line}:{edit.Column} [{edit.Confidence}]");
            }
        }

        if (duplicateEdits.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("DUPLICATE EDITS (same position appears twice):");
            report.AppendLine("-----------------------------------------------");

            foreach (var (renameCase, file, edit) in duplicateEdits)
            {
                report.AppendLine($"  [{renameCase}] {file}:{edit.Line}:{edit.Column} [{edit.Confidence}]");
            }
        }

        Console.WriteLine(report.ToString());

        // 6. Assert
        Assert.AreEqual(0, falsePositives.Count,
            $"Found {falsePositives.Count} false positive rename edits marked as # FP");

        Assert.AreEqual(0, unverifiedEntries.Count,
            $"Found {unverifiedEntries.Count} unverified rename edits. " +
            $"See RENAME_VERIFICATION_OUTPUT.txt and add # OK markers to RENAME_VERIFICATION_VERIFIED.txt");
    }

    private string GetRelativePath(string? fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        fullPath = fullPath.Replace('\\', '/');
        basePath = basePath.Replace('\\', '/');
        if (!basePath.EndsWith("/"))
            basePath += "/";

        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(basePath.Length);
        }

        return fullPath;
    }
}
