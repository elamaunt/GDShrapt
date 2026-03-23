using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.SmokeTests;

[TestClass]
[TestCategory("SmokeTests")]
public class GodotOpenRpgDeadCodeTests : SmokeTestBase
{
    private const string RepoUrl = "https://github.com/gdquest-demos/godot-open-rpg.git";
    private const string RepoName = "godot-open-rpg";
    private const string PinnedCommit = "7cd2deb44e6020d0bbca4a6bedfc7ed070bd2557";

    private static readonly string VerificationDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "verification"));
    private static readonly string OutputFile = Path.Combine(VerificationDir, "DEAD_CODE_OUTPUT.txt");
    private static readonly string VerifiedFile = Path.Combine(VerificationDir, "DEAD_CODE_VERIFIED.txt");
    private static readonly string AddonOutputFile = Path.Combine(VerificationDir, "DEAD_CODE_ADDON_OUTPUT.txt");
    private static readonly string AddonVerifiedFile = Path.Combine(VerificationDir, "DEAD_CODE_ADDON_VERIFIED.txt");

    [ClassInitialize]
    public static void Init(TestContext _) => InitProject(RepoUrl, RepoName, PinnedCommit);

    [ClassCleanup]
    public static void Cleanup() => CleanupProject();

    [TestMethod]
    [Timeout(120000)]
    public void DeadCode_AnalyzeProject_MatchesVerifiedBaseline()
    {
        Project.BuildCallSiteRegistry(CancellationToken.None);

        var projectModel = new GDProjectSemanticModel(Project);
        var handler = new GDDeadCodeHandler(projectModel);

        var options = new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.Strict,
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = true,
            IncludeUnreachable = true,
            CollectDroppedByReflection = true,
            TreatClassNameAsPublicAPI = false
        };

        var report = handler.AnalyzeProject(options);

        var reportText = GenerateReport(report, ProjectRoot);

        Directory.CreateDirectory(VerificationDir);
        File.WriteAllText(OutputFile, reportText);

        if (!File.Exists(VerifiedFile))
        {
            Assert.Fail($"Verified baseline not found: {VerifiedFile}\n" +
                        $"Review {OutputFile} and copy to {VerifiedFile} if correct.");
        }

        var verified = File.ReadAllText(VerifiedFile);
        reportText.Should().Be(verified,
            $"Dead code output differs from verified baseline.\n" +
            $"Output:   {OutputFile}\n" +
            $"Verified: {VerifiedFile}\n" +
            $"Review the diff and update the verified file if the change is intentional.");
    }

    [TestMethod]
    [Timeout(120000)]
    public void DeadCode_WithAddonPaths_MatchesVerifiedBaseline()
    {
        Project.BuildCallSiteRegistry(CancellationToken.None);

        var projectModel = new GDProjectSemanticModel(Project);
        var handler = new GDDeadCodeHandler(projectModel);

        var options = new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.Strict,
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = true,
            IncludeUnreachable = true,
            CollectDroppedByReflection = true,
            TreatClassNameAsPublicAPI = false,
            AddonPathsAsPublicAPI = new() { "addons/" }
        };

        var report = handler.AnalyzeProject(options);

        var reportText = GenerateReport(report, ProjectRoot);

        Directory.CreateDirectory(VerificationDir);
        File.WriteAllText(AddonOutputFile, reportText);

        if (!File.Exists(AddonVerifiedFile))
        {
            Assert.Fail($"Verified baseline not found: {AddonVerifiedFile}\n" +
                        $"Review {AddonOutputFile} and copy to {AddonVerifiedFile} if correct.");
        }

        var verified = File.ReadAllText(AddonVerifiedFile);
        reportText.Should().Be(verified,
            $"Dead code output differs from verified baseline.\n" +
            $"Output:   {AddonOutputFile}\n" +
            $"Verified: {AddonVerifiedFile}\n" +
            $"Review the diff and update the verified file if the change is intentional.");
    }

    private static string GenerateReport(GDDeadCodeReport report, string projectRoot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("DEAD CODE ANALYSIS: godot-open-rpg");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        sb.AppendLine("SUMMARY");
        sb.AppendLine($"Items: {report.Items.Count}");

        var byKind = report.Items.GroupBy(i => i.Kind).OrderBy(g => g.Key);
        foreach (var group in byKind)
            sb.AppendLine($"{group.Key}: {group.Count()}");

        sb.AppendLine();
        sb.AppendLine("BY FILE");
        sb.AppendLine("--------------------------------------------------------------------------------");

        var byFile = report.Items
            .GroupBy(i => Path.GetRelativePath(projectRoot, i.FilePath).Replace('\\', '/'))
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var fileGroup in byFile)
        {
            sb.AppendLine($"{fileGroup.Key}:");
            foreach (var item in fileGroup.OrderBy(i => i.Line).ThenBy(i => i.Column))
            {
                sb.AppendLine($"  {item.Line + 1}:{item.Column} {item.Kind}: {item.Name} [{item.ReasonCode}] [{item.Confidence}]");
            }
            sb.AppendLine();
        }

        if (report.DroppedByReflection.Count > 0)
        {
            sb.AppendLine("SUPPRESSED BY REFLECTION");
            sb.AppendLine("--------------------------------------------------------------------------------");
            var droppedByFile = report.DroppedByReflection
                .GroupBy(d => Path.GetRelativePath(projectRoot, d.FilePath).Replace('\\', '/'))
                .OrderBy(g => g.Key, StringComparer.Ordinal);
            foreach (var fileGroup in droppedByFile)
            {
                sb.AppendLine($"{fileGroup.Key}:");
                foreach (var item in fileGroup.OrderBy(i => i.Line))
                    sb.AppendLine($"  {item.Line + 1}:{item.Column} {item.Kind}: {item.Name} [{item.ReflectionKind}]");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
