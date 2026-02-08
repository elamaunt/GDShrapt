using System.Text;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Generates TYPE_SYSTEM_COMPREHENSIVE.txt — a read-only combined view of all type system aspects.
/// Not for verification, but for debugging and understanding type inference at each location.
/// </summary>
public class ComprehensiveOutputGenerator
{
    private readonly TypeNodeCollector _basicCollector = new();
    private readonly TypeInfoCollector _typeInfoCollector = new();
    private readonly DuckTypeCollector _duckTypeCollector = new();
    private readonly FlowNarrowingCollector _flowCollector = new();

    public void GenerateOutput(GDScriptProject project, string outputPath, string projectBasePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("TYPE SYSTEM COMPREHENSIVE REPORT");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        int totalFiles = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectBasePath);

            // Collect all aspects
            var typeInfoEntries = _typeInfoCollector.CollectEntries(file);
            var duckEntries = _duckTypeCollector.CollectEntries(file);
            var flowEntries = _flowCollector.CollectEntries(file);

            // Skip files with no data
            if (typeInfoEntries.Count == 0 && duckEntries.Count == 0 && flowEntries.Count == 0)
                continue;

            totalFiles++;

            sb.AppendLine(relativePath);
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // Index duck types by (line, col) for lookup
            var duckByPos = duckEntries.ToDictionary(d => (d.Line, d.Column));

            // Index flow narrowings by variable name for each method
            var flowByMethod = flowEntries
                .GroupBy(f => f.MethodName)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Output type info entries (declarations)
            foreach (var entry in typeInfoEntries)
            {
                sb.AppendLine($"--- {entry.Line}:{entry.Column} {entry.SymbolKind} {entry.Name} ---");

                // Basic type info
                sb.AppendLine($"  Type Info:");
                sb.AppendLine($"    declared:     {entry.DeclaredType ?? "(none)"}");
                sb.AppendLine($"    inferred:     {entry.InferredType}");
                sb.AppendLine($"    effective:    {entry.EffectiveType}");
                sb.AppendLine($"    confidence:   {entry.Confidence}");
                sb.AppendLine($"    nullable:     {(entry.IsNullable ? "yes" : "no")}");

                if (entry.IsPotentiallyNull)
                    sb.AppendLine($"    pot-null:     yes");

                if (entry.IsUnionType)
                    sb.AppendLine($"    union:        {entry.UnionMembers ?? "yes"}");

                if (entry.ContainerInfo != null)
                    sb.AppendLine($"    container:    {entry.ContainerInfo}");

                // Duck type info (if this is a parameter)
                if (duckByPos.TryGetValue((entry.Line, entry.Column), out var duckEntry))
                {
                    sb.AppendLine($"  Parameter Inference:");
                    sb.AppendLine($"    inferred:     {duckEntry.InferredType}");
                    sb.AppendLine($"    confidence:   {duckEntry.Confidence}");

                    if (duckEntry.Reason != null)
                        sb.AppendLine($"    reason:       {duckEntry.Reason}");

                    if (duckEntry.IsDuckTyped)
                        sb.AppendLine($"    duck-typed:   yes");

                    if (duckEntry.RequiredMethods != null || duckEntry.RequiredProperties != null || duckEntry.RequiredSignals != null)
                    {
                        sb.AppendLine($"  Duck Constraints:");
                        if (duckEntry.RequiredMethods != null)
                            sb.AppendLine($"    methods:      {duckEntry.RequiredMethods}");
                        if (duckEntry.RequiredProperties != null)
                            sb.AppendLine($"    properties:   {duckEntry.RequiredProperties}");
                        if (duckEntry.RequiredSignals != null)
                            sb.AppendLine($"    signals:      {duckEntry.RequiredSignals}");
                    }

                    if (duckEntry.PossibleTypes != null)
                        sb.AppendLine($"    possible:     {duckEntry.PossibleTypes}");
                    if (duckEntry.ExcludedTypes != null)
                        sb.AppendLine($"    excluded:     {duckEntry.ExcludedTypes}");
                    if (duckEntry.ConcreteType != null)
                        sb.AppendLine($"    concrete:     {duckEntry.ConcreteType}");
                }

                sb.AppendLine();
            }

            // Output flow narrowings (separately, grouped by method)
            if (flowEntries.Count > 0)
            {
                sb.AppendLine("  Flow Narrowing:");

                foreach (var methodGroup in flowByMethod)
                {
                    sb.AppendLine($"    {methodGroup.Key}():");
                    foreach (var flow in methodGroup.Value)
                    {
                        sb.AppendLine($"      {flow.Line}:{flow.Column} {flow.VariableName} -> {flow.NarrowedType} (base: {flow.BaseType})");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        sb.AppendLine($"# Summary: {totalFiles} files analyzed");

        File.WriteAllText(outputPath, sb.ToString());
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
