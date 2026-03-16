using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for type inference.
/// Generates TYPE_INFERENCE_OUTPUT.txt and compares with TYPE_INFERENCE_VERIFIED.txt.
/// Split into per-file tests via DynamicData for granular failure reporting.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class TypeInferenceVerificationTest
{
    private static string OutputPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "TYPE_INFERENCE_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "TYPE_INFERENCE_VERIFIED.txt");

    private static Dictionary<string, TypeVerificationParser.VerifiedEntry> _verifiedLookup = null!;
    private static bool _initialized;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (_initialized) return;

        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        // Always generate output artifact
        try
        {
            var generator = new TypeOutputGenerator();
            generator.GenerateOutput(project, OutputPath, projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to generate TYPE_INFERENCE_OUTPUT.txt: {ex.Message}");
        }

        // Parse verified file
        var parser = new TypeVerificationParser();
        parser.ParseFile(VerifiedPath);
        _verifiedLookup = parser.GetVerifiedLookup();

        // Generate unverified report
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
    public void TypeInference_File(string relativePath)
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;
        var file = FindFile(project, relativePath, projectPath);
        if (file == null)
        {
            Assert.Inconclusive($"File not found: {relativePath}");
            return;
        }

        var collector = new TypeNodeCollector();
        var nodes = collector.CollectNodes(file);

        var unverified = new List<string>();
        var falsePositives = new List<string>();

        foreach (var node in nodes)
        {
            var key = TypeVerificationParser.CreateKey(relativePath, node.Line, node.Column, node.NodeKind);

            if (!_verifiedLookup.TryGetValue(key, out var verified))
            {
                unverified.Add($"  {node.Line}:{node.Column} {node.NodeKind} {node.Name} -> {node.InferredType}");
            }
            else if (verified.Status == TypeVerificationParser.VerificationStatus.FP)
            {
                falsePositives.Add($"  {node.Line}:{node.Column} FP: expected {verified.Type}, got {node.InferredType}");
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
    public void TypeInference_Summary()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        var collector = new TypeNodeCollector();
        int total = 0, verifiedOk = 0, skipped = 0, unverifiedCount = 0, fpCount = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var nodes = collector.CollectNodes(file);

            foreach (var node in nodes)
            {
                total++;
                var key = TypeVerificationParser.CreateKey(relativePath, node.Line, node.Column, node.NodeKind);
                if (_verifiedLookup.TryGetValue(key, out var verified))
                {
                    switch (verified.Status)
                    {
                        case TypeVerificationParser.VerificationStatus.OK: verifiedOk++; break;
                        case TypeVerificationParser.VerificationStatus.FP: fpCount++; break;
                        case TypeVerificationParser.VerificationStatus.Skip: skipped++; break;
                        default: unverifiedCount++; break;
                    }
                }
                else
                {
                    unverifiedCount++;
                }
            }
        }

        Console.WriteLine("TYPE INFERENCE VERIFICATION SUMMARY");
        Console.WriteLine("===================================");
        Console.WriteLine($"Total nodes:       {total}");
        Console.WriteLine($"Verified (OK):     {verifiedOk}");
        Console.WriteLine($"Skipped:           {skipped}");
        Console.WriteLine($"Unverified:        {unverifiedCount}");
        Console.WriteLine($"False Positives:   {fpCount}");
    }

    private static void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, TypeVerificationParser.VerifiedEntry> verifiedLookup)
    {
        var collector = new TypeNodeCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# TYPE_INFERENCE_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to TYPE_INFERENCE_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null) continue;
            var relativePath = GetRelativePath(file.FullPath, projectPath);
            var nodes = collector.CollectNodes(file);

            var unverified = new List<TypeNodeCollector.TypedNode>();
            foreach (var node in nodes)
            {
                var key = TypeVerificationParser.CreateKey(relativePath, node.Line, node.Column, node.NodeKind);
                if (!verifiedLookup.ContainsKey(key))
                    unverified.Add(node);
            }

            if (unverified.Count > 0)
            {
                sb.AppendLine(relativePath);
                foreach (var node in unverified)
                    sb.AppendLine($"{node.Line}:{node.Column} {node.NodeKind} {node.Name} -> {node.InferredType}");
                sb.AppendLine();
            }
        }

        var unverifiedPath = Path.Combine(IntegrationTestHelpers.GetVerificationRoot(), "TYPE_INFERENCE_UNVERIFIED.txt");
        File.WriteAllText(unverifiedPath, sb.ToString());
    }

    private static GDScriptFile? FindFile(GDScriptProject project, string relativePath, string projectPath)
    {
        var fullPath = Path.Combine(projectPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
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
