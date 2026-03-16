using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for flow-sensitive type narrowing.
/// Generates FLOW_NARROWING_OUTPUT.txt and compares with FLOW_NARROWING_VERIFIED.txt.
/// Split into per-file tests via DynamicData for granular failure reporting.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class FlowNarrowingVerificationTest
{
    private static string OutputPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "FLOW_NARROWING_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "FLOW_NARROWING_VERIFIED.txt");

    private static Dictionary<string, FlowNarrowingVerificationParser.VerifiedNarrowing> _verifiedLookup = null!;
    private static bool _initialized;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (_initialized) return;

        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        try
        {
            var generator = new FlowNarrowingOutputGenerator();
            generator.GenerateOutput(project, OutputPath, projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to generate FLOW_NARROWING_OUTPUT.txt: {ex.Message}");
        }

        var parser = new FlowNarrowingVerificationParser();
        parser.ParseFile(VerifiedPath);
        _verifiedLookup = parser.GetVerifiedLookup();

        try
        {
            WriteUnverifiedReport(project, projectPath, _verifiedLookup);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to generate unverified report: {ex.Message}");
        }

        _initialized = true;
    }

    public static IEnumerable<object[]> GetScriptFiles()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
            var relativePath = GetRelativePath(file.FullPath, projectPath);
            yield return new object[] { relativePath };
        }
    }

    [TestMethod]
    [DynamicData(nameof(GetScriptFiles), DynamicDataSourceType.Method)]
    public void FlowNarrowing_File(string relativePath)
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;
        var file = FindFile(project, relativePath);
        if (file == null)
        {
            Assert.Inconclusive($"File not found: {relativePath}");
            return;
        }

        var collector = new FlowNarrowingCollector();
        var entries = collector.CollectEntries(file);

        var unverified = new List<string>();
        var falsePositives = new List<string>();

        foreach (var entry in entries)
        {
            var keyLine = $"{entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})";
            var key = FlowNarrowingVerificationParser.CreateKey(relativePath, entry.MethodName, keyLine);

            if (!_verifiedLookup.TryGetValue(key, out var verified))
            {
                unverified.Add($"  {entry.MethodName}(): {entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})");
            }
            else if (verified.Status == FlowNarrowingVerificationParser.VerificationStatus.FP)
            {
                falsePositives.Add($"  {entry.MethodName}(): {entry.Line}:{entry.Column} FP");
            }
        }

        var errors = new StringBuilder();
        if (unverified.Count > 0)
        {
            errors.AppendLine($"{unverified.Count} unverified:");
            foreach (var u in unverified.Take(20))
                errors.AppendLine(u);
            if (unverified.Count > 20)
                errors.AppendLine($"  ... and {unverified.Count - 20} more");
        }
        if (falsePositives.Count > 0)
        {
            errors.AppendLine($"{falsePositives.Count} false positive(s):");
            foreach (var fp in falsePositives)
                errors.AppendLine(fp);
        }

        if (errors.Length > 0)
        {
            Assert.Fail($"{relativePath}:\n{errors}");
        }
    }

    [TestMethod]
    public void FlowNarrowing_Summary()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        var collector = new FlowNarrowingCollector();
        int total = 0, verifiedOk = 0, skipped = 0, unverifiedCount = 0, fpCount = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var entries = collector.CollectEntries(file);

            foreach (var entry in entries)
            {
                total++;
                var keyLine = $"{entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})";
                var key = FlowNarrowingVerificationParser.CreateKey(relativePath, entry.MethodName, keyLine);

                if (_verifiedLookup.TryGetValue(key, out var verified))
                {
                    switch (verified.Status)
                    {
                        case FlowNarrowingVerificationParser.VerificationStatus.OK: verifiedOk++; break;
                        case FlowNarrowingVerificationParser.VerificationStatus.FP: fpCount++; break;
                        case FlowNarrowingVerificationParser.VerificationStatus.Skip: skipped++; break;
                        default: unverifiedCount++; break;
                    }
                }
                else
                {
                    unverifiedCount++;
                }
            }
        }

        Console.WriteLine("FLOW NARROWING VERIFICATION SUMMARY");
        Console.WriteLine("====================================");
        Console.WriteLine($"Total narrowings:  {total}");
        Console.WriteLine($"Verified (OK):     {verifiedOk}");
        Console.WriteLine($"Skipped:           {skipped}");
        Console.WriteLine($"Unverified:        {unverifiedCount}");
        Console.WriteLine($"False Positives:   {fpCount}");
    }

    private static void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, FlowNarrowingVerificationParser.VerifiedNarrowing> verifiedLookup)
    {
        var collector = new FlowNarrowingCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# FLOW_NARROWING_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to FLOW_NARROWING_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
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
                        sb.AppendLine($"    {entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})");
                    sb.AppendLine();
                }
            }
        }

        var unverifiedPath = Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "FLOW_NARROWING_UNVERIFIED.txt");
        File.WriteAllText(unverifiedPath, sb.ToString());
    }

    private static GDScriptFile? FindFile(GDScriptProject project, string relativePath)
    {
        return project.ScriptFiles.FirstOrDefault(f =>
            f.FullPath != null &&
            f.FullPath.Replace('\\', '/').EndsWith(relativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRelativePath(string fullPath, string basePath)
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
