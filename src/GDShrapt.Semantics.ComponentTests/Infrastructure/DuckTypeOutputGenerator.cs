using System.Text;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Generates DUCK_TYPES_OUTPUT.txt with parameter inference and duck type constraints.
/// </summary>
public class DuckTypeOutputGenerator
{
    private readonly DuckTypeCollector _collector = new();

    public void GenerateOutput(GDScriptProject project, string outputPath, string projectBasePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DUCK_TYPES_OUTPUT.txt");
        sb.AppendLine("# Auto-generated - do not edit manually");
        sb.AppendLine("# Format: block per parameter with inference + duck type details");
        sb.AppendLine();

        int totalEntries = 0;
        int totalFiles = 0;
        int duckTypedCount = 0;

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
                if (entry.IsDuckTyped || entry.RequiredMethods != null || entry.RequiredProperties != null)
                    duckTypedCount++;
            }

            sb.AppendLine();
        }

        sb.AppendLine($"# Summary: {totalEntries} parameters in {totalFiles} files ({duckTypedCount} duck-typed)");

        File.WriteAllText(outputPath, sb.ToString());
    }

    private void FormatEntry(StringBuilder sb, DuckTypeCollector.DuckTypeEntry entry)
    {
        // Key line (2-space indent)
        sb.AppendLine($"  {entry.Line}:{entry.Column} Parameter {entry.ParameterName} in {entry.MethodName}");

        // Detail lines (4-space indent)
        sb.AppendLine($"    inferred-type: {entry.InferredType}");
        sb.AppendLine($"    confidence: {entry.Confidence}");

        if (entry.Reason != null)
            sb.AppendLine($"    reason: {entry.Reason}");

        if (entry.IsDuckTyped)
            sb.AppendLine($"    duck-typed: yes");

        if (entry.IsUnion)
            sb.AppendLine($"    union: yes");

        // Duck constraints section
        bool hasDuckConstraints = entry.RequiredMethods != null ||
                                   entry.RequiredProperties != null ||
                                   entry.RequiredSignals != null;

        if (hasDuckConstraints)
        {
            sb.AppendLine($"    duck-constraints:");
            sb.AppendLine($"      methods: {entry.RequiredMethods ?? "(none)"}");
            sb.AppendLine($"      properties: {entry.RequiredProperties ?? "(none)"}");
            sb.AppendLine($"      signals: {entry.RequiredSignals ?? "(none)"}");
        }

        if (entry.PossibleTypes != null)
            sb.AppendLine($"    possible-types: {entry.PossibleTypes}");

        if (entry.ExcludedTypes != null)
            sb.AppendLine($"    excluded-types: {entry.ExcludedTypes}");

        if (entry.ConcreteType != null)
            sb.AppendLine($"    concrete-type: {entry.ConcreteType}");

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
