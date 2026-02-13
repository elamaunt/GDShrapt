using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// TDD verification test for rich type info (declared/inferred/narrowed/confidence/nullability).
/// Generates TYPE_INFO_OUTPUT.txt and compares with TYPE_INFO_VERIFIED.txt.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class TypeInfoVerificationTest
{
    private static string GetVerificationRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Could not find repo root"), "verification");
    }

    private static string OutputPath => Path.Combine(GetVerificationRoot(), "TYPE_INFO_OUTPUT.txt");
    private static string VerifiedPath => Path.Combine(GetVerificationRoot(), "TYPE_INFO_VERIFIED.txt");

    [TestMethod]
    public void AllDeclarations_MustHaveVerifiedTypeInfo()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        // 1. Generate output
        var generator = new TypeInfoOutputGenerator();
        generator.GenerateOutput(project, OutputPath, projectPath);

        // 2. Parse verification file
        var parser = new BlockVerificationParser();
        parser.ParseFile(VerifiedPath);
        var verifiedLookup = parser.GetVerifiedLookup();

        // 3. Compare
        var collector = new TypeInfoCollector();
        var unverifiedEntries = new List<(string file, TypeInfoCollector.TypeInfoEntry entry)>();
        var falsePositives = new List<(string file, TypeInfoCollector.TypeInfoEntry entry, BlockVerificationParser.VerifiedBlock block)>();
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
                var keyLine = $"{entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}";
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
        report.AppendLine("TYPE INFO VERIFICATION REPORT");
        report.AppendLine("============================");
        report.AppendLine($"Total entries:     {totalEntries}");
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
                report.AppendLine($"  {entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}");
                report.AppendLine($"    declared: {entry.DeclaredType ?? "(none)"} | inferred: {entry.InferredType} | effective: {entry.EffectiveType} | confidence: {entry.Confidence}");
            }
        }

        if (falsePositives.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("FALSE POSITIVES:");
            report.AppendLine("----------------");

            foreach (var (file, entry, block) in falsePositives)
            {
                report.AppendLine($"  {file}:{entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}");
                report.AppendLine($"    Actual: declared={entry.DeclaredType ?? "(none)"} inferred={entry.InferredType} effective={entry.EffectiveType}");
            }
        }

        Console.WriteLine(report.ToString());

        // 5. Write unverified report
        WriteUnverifiedReport(project, projectPath, verifiedLookup);

        // 6. Assert
        Assert.AreEqual(0, unverifiedEntries.Count,
            $"Found {unverifiedEntries.Count} unverified type info entries. " +
            $"See TYPE_INFO_OUTPUT.txt and add # OK markers to TYPE_INFO_VERIFIED.txt");
    }

    private void WriteUnverifiedReport(GDScriptProject project, string projectPath, Dictionary<string, BlockVerificationParser.VerifiedBlock> verifiedLookup)
    {
        var collector = new TypeInfoCollector();
        var sb = new StringBuilder();
        sb.AppendLine("# TYPE_INFO_UNVERIFIED.txt");
        sb.AppendLine("# Copy entries to TYPE_INFO_VERIFIED.txt and add # OK");
        sb.AppendLine();

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

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

        var unverifiedPath = Path.Combine(GetVerificationRoot(), "TYPE_INFO_UNVERIFIED.txt");
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
