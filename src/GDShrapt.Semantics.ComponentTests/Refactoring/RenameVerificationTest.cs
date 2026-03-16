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
/// Split into per-case tests via DynamicData for granular failure reporting.
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

    private static List<(RenameTestCase TestCase, GDRenameResult Result)> _results = null!;
    private static IReadOnlyDictionary<string, RenameVerificationParser.VerifiedEdit> _verifiedLookup = null!;
    private static string _projectPath = null!;
    private static bool _initialized;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (_initialized) return;

        var project = TestProjectFixture.Project;
        _projectPath = TestProjectFixture.ProjectPath;
        var service = TestProjectFixture.ProjectModel.Services.Rename;

        // Run all rename plans
        _results = new List<(RenameTestCase, GDRenameResult)>();
        foreach (var tc in TestCases)
        {
            var script = TestProjectFixture.GetScript(tc.SourceFile);
            GDRenameResult result;
            var symbol = script?.SemanticModel?.FindSymbol(tc.OldName);

            if (symbol != null)
                result = service.PlanRename(symbol, tc.NewName);
            else
                result = service.PlanRename(tc.OldName, tc.NewName, script?.FullPath);

            _results.Add((tc, result));
        }

        // Generate output artifact
        try
        {
            var generator = new RenameOutputGenerator();
            generator.GenerateOutput(_results, OutputPath, _projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to generate RENAME_VERIFICATION_OUTPUT.txt: {ex.Message}");
        }

        // Parse verified file
        var parser = new RenameVerificationParser();
        parser.ParseFile(VerifiedPath);
        _verifiedLookup = parser.VerifiedEdits;

        _initialized = true;
    }

    public static IEnumerable<object[]> GetRenameCases()
    {
        // Force fixture initialization via property access
        _ = TestProjectFixture.Project;

        foreach (var tc in TestCases)
        {
            yield return new object[] { tc.OldName, tc.NewName, tc.SourceFile };
        }
    }

    [TestMethod]
    [DynamicData(nameof(GetRenameCases), DynamicDataSourceType.Method)]
    public void Rename_Case(string oldName, string newName, string sourceFile)
    {
        var renameCase = $"{oldName}->{newName}";
        var (tc, result) = _results.First(r => r.TestCase.OldName == oldName && r.TestCase.NewName == newName);

        var unverified = new List<string>();
        var falsePositives = new List<string>();
        var seenKeys = new HashSet<string>();

        foreach (var edit in result.StrictEdits.Concat(result.PotentialEdits))
        {
            var relativePath = GetRelativePath(edit.FilePath, _projectPath);
            var key = RenameVerificationParser.CreateKey(renameCase, relativePath, edit.Line, edit.Column);

            if (!seenKeys.Add(key))
                continue;

            if (!_verifiedLookup.TryGetValue(key, out var verified))
            {
                var reason = !string.IsNullOrEmpty(edit.ConfidenceReason) ? $" ({edit.ConfidenceReason})" : "";
                unverified.Add($"  {relativePath}:{edit.Line}:{edit.Column} [{edit.Confidence}]{reason}");
            }
            else if (verified.Status == RenameVerificationParser.VerificationStatus.FP)
            {
                falsePositives.Add($"  {relativePath}:{edit.Line}:{edit.Column} [{edit.Confidence}] FP");
            }
        }

        var errors = new StringBuilder();
        if (unverified.Count > 0)
        {
            errors.AppendLine($"{unverified.Count} unverified edit(s):");
            foreach (var u in unverified)
                errors.AppendLine(u);
        }
        if (falsePositives.Count > 0)
        {
            errors.AppendLine($"{falsePositives.Count} false positive(s):");
            foreach (var fp in falsePositives)
                errors.AppendLine(fp);
        }

        if (errors.Length > 0)
        {
            Assert.Fail($"[{renameCase}]:\n{errors}");
        }
    }

    [TestMethod]
    public void Rename_Summary()
    {
        int totalEdits = 0, verifiedOk = 0, skipped = 0, unverifiedCount = 0, fpCount = 0, duplicates = 0;
        var seenKeys = new HashSet<string>();

        foreach (var (tc, result) in _results)
        {
            var renameCase = $"{tc.OldName}->{tc.NewName}";

            foreach (var edit in result.StrictEdits.Concat(result.PotentialEdits))
            {
                totalEdits++;
                var relativePath = GetRelativePath(edit.FilePath, _projectPath);
                var key = RenameVerificationParser.CreateKey(renameCase, relativePath, edit.Line, edit.Column);

                if (!seenKeys.Add(key))
                {
                    duplicates++;
                    continue;
                }

                if (_verifiedLookup.TryGetValue(key, out var verified))
                {
                    switch (verified.Status)
                    {
                        case RenameVerificationParser.VerificationStatus.OK: verifiedOk++; break;
                        case RenameVerificationParser.VerificationStatus.FP: fpCount++; break;
                        case RenameVerificationParser.VerificationStatus.Skip: skipped++; break;
                        default: unverifiedCount++; break;
                    }
                }
                else
                {
                    unverifiedCount++;
                }
            }
        }

        Console.WriteLine("RENAME VERIFICATION SUMMARY");
        Console.WriteLine("===========================");
        Console.WriteLine($"Total edits:       {totalEdits}");
        Console.WriteLine($"Verified (OK):     {verifiedOk}");
        Console.WriteLine($"Skipped:           {skipped}");
        Console.WriteLine($"Unverified:        {unverifiedCount}");
        Console.WriteLine($"False Positives:   {fpCount}");
        Console.WriteLine($"Duplicate edits:   {duplicates}");
    }

    private static string GetRelativePath(string? fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        fullPath = fullPath.Replace('\\', '/');
        basePath = basePath.Replace('\\', '/');
        if (!basePath.EndsWith("/"))
            basePath += "/";

        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(basePath.Length);

        return fullPath;
    }
}
