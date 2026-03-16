using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for rich type info (declared/inferred/narrowed/confidence/nullability).
/// Generates TYPE_INFO_OUTPUT.txt and compares with TYPE_INFO_VERIFIED.txt.
/// Split into per-file tests via DynamicData for granular failure reporting.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class TypeInfoVerificationTest
{
    private static string OutputPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "TYPE_INFO_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "TYPE_INFO_VERIFIED.txt");

    private static Dictionary<string, BlockVerificationParser.VerifiedBlock> _verifiedLookup = null!;
    private static bool _initialized;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (_initialized) return;

        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        try
        {
            var generator = new TypeInfoOutputGenerator();
            generator.GenerateOutput(project, OutputPath, projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to generate TYPE_INFO_OUTPUT.txt: {ex.Message}");
        }

        var parser = new BlockVerificationParser();
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
    public void TypeInfo_File(string relativePath)
    {
        var project = TestProjectFixture.Project;
        var file = FindFile(project, relativePath);
        if (file == null)
        {
            Assert.Inconclusive($"File not found: {relativePath}");
            return;
        }

        var collector = new TypeInfoCollector();
        var entries = collector.CollectEntries(file);

        var unverified = new List<string>();
        var falsePositives = new List<string>();

        foreach (var entry in entries)
        {
            var keyLine = $"{entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}";
            var key = BlockVerificationParser.CreateKey(relativePath, keyLine);

            if (!_verifiedLookup.TryGetValue(key, out var block))
            {
                unverified.Add($"  {entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name} | declared: {entry.DeclaredType ?? "(none)"} | inferred: {entry.InferredType} | effective: {entry.EffectiveType}");
            }
            else if (block.Status == BlockVerificationParser.VerificationStatus.FP)
            {
                falsePositives.Add($"  {entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name} FP");
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
    public void TypeInfo_Summary()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        var collector = new TypeInfoCollector();
        int total = 0, verifiedOk = 0, skipped = 0, unverifiedCount = 0, fpCount = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var entries = collector.CollectEntries(file);

            foreach (var entry in entries)
            {
                total++;
                var keyLine = $"{entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}";
                var key = BlockVerificationParser.CreateKey(relativePath, keyLine);

                if (_verifiedLookup.TryGetValue(key, out var block))
                {
                    switch (block.Status)
                    {
                        case BlockVerificationParser.VerificationStatus.OK: verifiedOk++; break;
                        case BlockVerificationParser.VerificationStatus.FP: fpCount++; break;
                        case BlockVerificationParser.VerificationStatus.Skip: skipped++; break;
                        default: unverifiedCount++; break;
                    }
                }
                else
                {
                    unverifiedCount++;
                }
            }
        }

        Console.WriteLine("TYPE INFO VERIFICATION SUMMARY");
        Console.WriteLine("==============================");
        Console.WriteLine($"Total entries:     {total}");
        Console.WriteLine($"Verified (OK):     {verifiedOk}");
        Console.WriteLine($"Skipped:           {skipped}");
        Console.WriteLine($"Unverified:        {unverifiedCount}");
        Console.WriteLine($"False Positives:   {fpCount}");
    }

    private static void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, BlockVerificationParser.VerifiedBlock> verifiedLookup)
    {
        var collector = new TypeInfoCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# TYPE_INFO_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to TYPE_INFO_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var entries = collector.CollectEntries(file);

            var unverified = new List<TypeInfoCollector.TypeInfoEntry>();
            foreach (var entry in entries)
            {
                var keyLine = $"{entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}";
                var key = BlockVerificationParser.CreateKey(relativePath, keyLine);
                if (!verifiedLookup.ContainsKey(key))
                    unverified.Add(entry);
            }

            if (unverified.Count > 0)
            {
                sb.AppendLine(relativePath);
                foreach (var entry in unverified)
                {
                    sb.AppendLine($"  {entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}");
                    sb.AppendLine($"    declared: {entry.DeclaredType ?? "(none)"}");
                    sb.AppendLine($"    inferred: {entry.InferredType}");
                    sb.AppendLine($"    effective: {entry.EffectiveType}");
                    sb.AppendLine($"    confidence: {entry.Confidence}");
                    sb.AppendLine($"    nullable: {(entry.IsNullable ? "yes" : "no")}");
                    if (entry.IsPotentiallyNull)
                        sb.AppendLine($"    potentially-null: yes");
                    if (entry.IsUnionType)
                        sb.AppendLine($"    union: {entry.UnionMembers ?? "yes"}");
                    if (entry.ContainerInfo != null)
                        sb.AppendLine($"    container: {entry.ContainerInfo}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
        }

        var unverifiedPath = Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "TYPE_INFO_UNVERIFIED.txt");
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
