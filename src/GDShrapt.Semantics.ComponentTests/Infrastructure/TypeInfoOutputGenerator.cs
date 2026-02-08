using System.Text;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Generates TYPE_INFO_OUTPUT.txt with rich type information for all declarations.
/// </summary>
public class TypeInfoOutputGenerator
{
    private readonly TypeInfoCollector _collector = new();

    public void GenerateOutput(GDScriptProject project, string outputPath, string projectBasePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TYPE_INFO_OUTPUT.txt");
        sb.AppendLine("# Auto-generated - do not edit manually");
        sb.AppendLine("# Format: block per declaration with type details");
        sb.AppendLine();

        int totalEntries = 0;
        int totalFiles = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var entries = _collector.CollectEntries(file);
            if (entries.Count == 0)
                continue;

            totalFiles++;
            totalEntries += entries.Count;

            var relativePath = GetRelativePath(file.FullPath, projectBasePath);
            sb.AppendLine(relativePath);

            foreach (var entry in entries)
            {
                FormatEntry(sb, entry);
            }

            sb.AppendLine();
        }

        sb.AppendLine($"# Summary: {totalEntries} entries in {totalFiles} files");

        File.WriteAllText(outputPath, sb.ToString());
    }

    private void FormatEntry(StringBuilder sb, TypeInfoCollector.TypeInfoEntry entry)
    {
        // Key line (2-space indent)
        sb.AppendLine($"  {entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name}");

        // Detail lines (4-space indent)
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

    private string GetRelativePath(string? fullPath, string basePath)
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
