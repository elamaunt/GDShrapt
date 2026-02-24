using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for flow-sensitive type narrowing.
/// Generates FLOW_NARROWING_OUTPUT.txt and compares with FLOW_NARROWING_VERIFIED.txt.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class FlowNarrowingVerificationTest
{
    private static string OutputPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "FLOW_NARROWING_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "FLOW_NARROWING_VERIFIED.txt");

    [TestMethod]
    public void AllNarrowedTypes_MustBeVerified()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        // 1. Generate output
        var generator = new FlowNarrowingOutputGenerator();
        generator.GenerateOutput(project, OutputPath, projectPath);

        // 2. Parse verification file
        var parser = new FlowNarrowingVerificationParser();
        parser.ParseFile(VerifiedPath);
        var verifiedLookup = parser.GetVerifiedLookup();

        // 3. Compare
        var collector = new FlowNarrowingCollector();
        var unverifiedEntries = new List<(string file, FlowNarrowingCollector.NarrowingEntry entry)>();
        var falsePositives = new List<(string file, FlowNarrowingCollector.NarrowingEntry entry)>();
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
                var keyLine = $"{entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})";
                var key = FlowNarrowingVerificationParser.CreateKey(relativePath, entry.MethodName, keyLine);

                if (verifiedLookup.TryGetValue(key, out var verified))
                {
                    switch (verified.Status)
                    {
                        case FlowNarrowingVerificationParser.VerificationStatus.OK:
                            verifiedOk++;
                            break;
                        case FlowNarrowingVerificationParser.VerificationStatus.FP:
                            falsePositives.Add((relativePath, entry));
                            break;
                        case FlowNarrowingVerificationParser.VerificationStatus.Skip:
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
        report.AppendLine("FLOW NARROWING VERIFICATION REPORT");
        report.AppendLine("==================================");
        report.AppendLine($"Total narrowings:  {totalEntries}");
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
                report.AppendLine($"  {entry.MethodName}(): {entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})");
            }
        }

        Console.WriteLine(report.ToString());

        // 5. Write unverified report
        WriteUnverifiedReport(project, projectPath, verifiedLookup);

        // 6. Assert
        Assert.AreEqual(0, unverifiedEntries.Count,
            $"Found {unverifiedEntries.Count} unverified flow narrowing entries. " +
            $"See FLOW_NARROWING_OUTPUT.txt and add # OK markers to FLOW_NARROWING_VERIFIED.txt");
    }

    private void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, FlowNarrowingVerificationParser.VerifiedNarrowing> verifiedLookup)
    {
        var collector = new FlowNarrowingCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# FLOW_NARROWING_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to FLOW_NARROWING_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var entries = collector.CollectEntries(file);

            var unverified = new List<FlowNarrowingCollector.NarrowingEntry>();
            foreach (var entry in entries)
            {
                var keyLine = $"{entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})";
                var key = FlowNarrowingVerificationParser.CreateKey(relativePath, entry.MethodName, keyLine);
                if (!verifiedLookup.ContainsKey(key))
                    unverified.Add(entry);
            }

            if (unverified.Count > 0)
            {
                sb.AppendLine(relativePath);

                foreach (var methodGroup in unverified.GroupBy(e => e.MethodName))
                {
                    sb.AppendLine($"  {methodGroup.Key}()");
                    foreach (var entry in methodGroup)
                    {
                        sb.AppendLine($"    {entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})");
                    }
                    sb.AppendLine();
                }
            }
        }

        var unverifiedPath = Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "FLOW_NARROWING_UNVERIFIED.txt");
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
