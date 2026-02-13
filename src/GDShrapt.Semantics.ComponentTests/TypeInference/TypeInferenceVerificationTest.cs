using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for type inference.
/// Generates TYPE_INFERENCE_OUTPUT.txt and compares with TYPE_INFERENCE_VERIFIED.txt.
/// Test fails until all entries are verified with # OK marker.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class TypeInferenceVerificationTest
{
    private static string GetVerificationRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Could not find repo root"), "verification");
    }

    private static string OutputPath => Path.Combine(GetVerificationRoot(), "TYPE_INFERENCE_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(GetVerificationRoot(), "TYPE_INFERENCE_VERIFIED.txt");

    [TestMethod]
    public void AllNodes_MustHaveVerifiedTypes()
    {
        // 1. Get project
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        // 2. Generate output file
        var generator = new TypeOutputGenerator();
        generator.GenerateOutput(project, OutputPath, projectPath);

        // 3. Parse verification file
        var parser = new TypeVerificationParser();
        parser.ParseFile(VerifiedPath);
        var verifiedLookup = parser.GetVerifiedLookup();

        // 4. Compare and collect statistics
        var collector = new TypeNodeCollector();
        var unverifiedEntries = new List<(string file, TypeNodeCollector.TypedNode node)>();
        var falsePositives = new List<(string file, TypeNodeCollector.TypedNode node, TypeVerificationParser.VerifiedEntry verified)>();
        int totalNodes = 0;
        int verifiedOk = 0;
        int skipped = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var nodes = collector.CollectNodes(file);

            foreach (var node in nodes)
            {
                totalNodes++;
                var key = TypeVerificationParser.CreateKey(relativePath, node.Line, node.Column, node.NodeKind);

                if (verifiedLookup.TryGetValue(key, out var verified))
                {
                    switch (verified.Status)
                    {
                        case TypeVerificationParser.VerificationStatus.OK:
                            verifiedOk++;
                            break;
                        case TypeVerificationParser.VerificationStatus.FP:
                            falsePositives.Add((relativePath, node, verified));
                            break;
                        case TypeVerificationParser.VerificationStatus.Skip:
                            skipped++;
                            break;
                        default:
                            unverifiedEntries.Add((relativePath, node));
                            break;
                    }
                }
                else
                {
                    unverifiedEntries.Add((relativePath, node));
                }
            }
        }

        // 5. Generate report
        var report = new StringBuilder();
        report.AppendLine("TYPE INFERENCE VERIFICATION REPORT");
        report.AppendLine("==================================");
        report.AppendLine($"Total nodes:       {totalNodes}");
        report.AppendLine($"Verified (OK):     {verifiedOk}");
        report.AppendLine($"Skipped:           {skipped}");
        report.AppendLine($"Unverified:        {unverifiedEntries.Count}");
        report.AppendLine($"False Positives:   {falsePositives.Count}");
        report.AppendLine();

        if (unverifiedEntries.Count > 0)
        {
            report.AppendLine("UNVERIFIED ENTRIES (first 50):");
            report.AppendLine("------------------------------");

            string? lastFile = null;
            foreach (var (file, node) in unverifiedEntries.Take(50))
            {
                if (file != lastFile)
                {
                    report.AppendLine();
                    report.AppendLine(file);
                    lastFile = file;
                }
                report.AppendLine($"  {node.Line}:{node.Column} {node.NodeKind} {node.Name} -> {node.InferredType}");
            }

            if (unverifiedEntries.Count > 50)
            {
                report.AppendLine();
                report.AppendLine($"... and {unverifiedEntries.Count - 50} more unverified entries");
            }
        }

        if (falsePositives.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("FALSE POSITIVES:");
            report.AppendLine("----------------");

            foreach (var (file, node, verified) in falsePositives)
            {
                report.AppendLine($"  {file}:{node.Line}:{node.Column}");
                report.AppendLine($"    Expected (verified): {verified.Type}");
                report.AppendLine($"    Actual (inferred):   {node.InferredType}");
            }
        }

        Console.WriteLine(report.ToString());

        // 6. Write full unverified report to file for easy copy-paste
        WriteUnverifiedReport(project, projectPath, verifiedLookup);

        // 7. Assert
        Assert.AreEqual(0, unverifiedEntries.Count,
            $"Found {unverifiedEntries.Count} unverified type inference entries. " +
            $"See TYPE_INFERENCE_OUTPUT.txt and add # OK markers to TYPE_INFERENCE_VERIFIED.txt");
    }

    private void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, TypeVerificationParser.VerifiedEntry> verifiedLookup)
    {
        var collector = new TypeNodeCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# TYPE_INFERENCE_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to TYPE_INFERENCE_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var nodes = collector.CollectNodes(file);

            var unverified = new List<TypeNodeCollector.TypedNode>();
            foreach (var node in nodes)
            {
                var key = TypeVerificationParser.CreateKey(relativePath, node.Line, node.Column, node.NodeKind);
                if (!verifiedLookup.ContainsKey(key))
                {
                    unverified.Add(node);
                }
            }

            if (unverified.Count > 0)
            {
                sb.AppendLine(relativePath);
                foreach (var node in unverified)
                {
                    sb.AppendLine($"{node.Line}:{node.Column} {node.NodeKind} {node.Name} -> {node.InferredType}");
                }
                sb.AppendLine();
            }
        }

        var unverifiedPath = Path.Combine(GetVerificationRoot(), "TYPE_INFERENCE_UNVERIFIED.txt");
        File.WriteAllText(unverifiedPath, sb.ToString());
    }

    private string GetRelativePath(string fullPath, string basePath)
    {
        // Normalize paths to forward slashes and ensure trailing slash on basePath
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
