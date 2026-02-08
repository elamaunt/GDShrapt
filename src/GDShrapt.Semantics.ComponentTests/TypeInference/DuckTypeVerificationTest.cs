using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for duck typing and parameter inference.
/// Generates DUCK_TYPES_OUTPUT.txt and compares with DUCK_TYPES_VERIFIED.txt.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class DuckTypeVerificationTest
{
    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GDShrapt.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root");
    }

    private static string OutputPath => Path.Combine(GetSolutionRoot(), "DUCK_TYPES_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(GetSolutionRoot(), "DUCK_TYPES_VERIFIED.txt");

    [TestMethod]
    public void AllParameters_MustHaveVerifiedDuckTypes()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        // 1. Generate output
        var generator = new DuckTypeOutputGenerator();
        generator.GenerateOutput(project, OutputPath, projectPath);

        // 2. Parse verification file
        var parser = new BlockVerificationParser();
        parser.ParseFile(VerifiedPath);
        var verifiedLookup = parser.GetVerifiedLookup();

        // 3. Compare
        var collector = new DuckTypeCollector();
        var unverifiedEntries = new List<(string file, DuckTypeCollector.DuckTypeEntry entry)>();
        var falsePositives = new List<(string file, DuckTypeCollector.DuckTypeEntry entry, BlockVerificationParser.VerifiedBlock block)>();
        int totalEntries = 0;
        int verifiedOk = 0;
        int skipped = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var entries = collector.CollectEntries(file);

            foreach (var entry in entries)
            {
                totalEntries++;
                var keyLine = $"{entry.Line}:{entry.Column} Parameter {entry.ParameterName} in {entry.MethodName}";
                var key = BlockVerificationParser.CreateKey(relativePath, keyLine);

                if (verifiedLookup.TryGetValue(key, out var block))
                {
                    switch (block.Status)
                    {
                        case BlockVerificationParser.VerificationStatus.OK:
                            verifiedOk++;
                            break;
                        case BlockVerificationParser.VerificationStatus.FP:
                            falsePositives.Add((relativePath, entry, block));
                            break;
                        case BlockVerificationParser.VerificationStatus.Skip:
                            skipped++;
                            break;
                        default:
                            unverifiedEntries.Add((relativePath, entry));
                            break;
                    }
                }
                else
                {
                    unverifiedEntries.Add((relativePath, entry));
                }
            }
        }

        // 4. Report
        var report = new StringBuilder();
        report.AppendLine("DUCK TYPES VERIFICATION REPORT");
        report.AppendLine("==============================");
        report.AppendLine($"Total parameters:  {totalEntries}");
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
            foreach (var (file, entry) in unverifiedEntries.Take(50))
            {
                if (file != lastFile)
                {
                    report.AppendLine();
                    report.AppendLine(file);
                    lastFile = file;
                }
                report.AppendLine($"  {entry.Line}:{entry.Column} Parameter {entry.ParameterName} in {entry.MethodName}");
                report.AppendLine($"    inferred: {entry.InferredType} | confidence: {entry.Confidence} | duck: {entry.IsDuckTyped}");
                if (entry.RequiredMethods != null)
                    report.AppendLine($"    methods: {entry.RequiredMethods}");
            }
        }

        Console.WriteLine(report.ToString());

        // 5. Write unverified report
        WriteUnverifiedReport(project, projectPath, verifiedLookup);

        // 6. Assert
        Assert.AreEqual(0, unverifiedEntries.Count,
            $"Found {unverifiedEntries.Count} unverified duck type entries. " +
            $"See DUCK_TYPES_OUTPUT.txt and add # OK markers to DUCK_TYPES_VERIFIED.txt");
    }

    private void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, BlockVerificationParser.VerifiedBlock> verifiedLookup)
    {
        var collector = new DuckTypeCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# DUCK_TYPES_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to DUCK_TYPES_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var entries = collector.CollectEntries(file);

            var unverified = new List<DuckTypeCollector.DuckTypeEntry>();
            foreach (var entry in entries)
            {
                var keyLine = $"{entry.Line}:{entry.Column} Parameter {entry.ParameterName} in {entry.MethodName}";
                var key = BlockVerificationParser.CreateKey(relativePath, keyLine);
                if (!verifiedLookup.ContainsKey(key))
                    unverified.Add(entry);
            }

            if (unverified.Count > 0)
            {
                sb.AppendLine(relativePath);
                foreach (var entry in unverified)
                {
                    sb.AppendLine($"  {entry.Line}:{entry.Column} Parameter {entry.ParameterName} in {entry.MethodName}");
                    sb.AppendLine($"    inferred-type: {entry.InferredType}");
                    sb.AppendLine($"    confidence: {entry.Confidence}");
                    if (entry.Reason != null)
                        sb.AppendLine($"    reason: {entry.Reason}");
                    if (entry.IsDuckTyped)
                        sb.AppendLine($"    duck-typed: yes");
                    if (entry.RequiredMethods != null)
                        sb.AppendLine($"    methods: {entry.RequiredMethods}");
                    if (entry.RequiredProperties != null)
                        sb.AppendLine($"    properties: {entry.RequiredProperties}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
        }

        var unverifiedPath = Path.Combine(GetSolutionRoot(), "DUCK_TYPES_UNVERIFIED.txt");
        File.WriteAllText(unverifiedPath, sb.ToString());
    }

    private string GetRelativePath(string fullPath, string basePath)
    {
        fullPath = fullPath.Replace('\\', '/');
        basePath = basePath.Replace('\\', '/');
        if (!basePath.EndsWith("/"))
            basePath += "/";

        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(basePath.Length);

        return fullPath;
    }
}
