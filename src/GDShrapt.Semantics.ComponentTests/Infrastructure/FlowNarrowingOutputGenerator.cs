using System.Text;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Generates FLOW_NARROWING_OUTPUT.txt with type narrowing entries.
/// Groups entries by method, showing how variable types narrow within each method.
/// </summary>
public class FlowNarrowingOutputGenerator
{
    private readonly FlowNarrowingCollector _collector = new();

    public void GenerateOutput(GDScriptProject project, string outputPath, string projectBasePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# FLOW_NARROWING_OUTPUT.txt");
        sb.AppendLine("# Auto-generated - do not edit manually");
        sb.AppendLine("# Format: method → narrowed identifiers within it");
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

            // Group by method
            foreach (var methodGroup in entries.GroupBy(e => e.MethodName))
            {
                sb.AppendLine($"  {methodGroup.Key}()");

                foreach (var entry in methodGroup)
                {
                    sb.AppendLine($"    {entry.Line}:{entry.Column} {entry.VariableName} -> {entry.NarrowedType} (base: {entry.BaseType})");
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine($"# Summary: {totalEntries} narrowings in {totalFiles} files");

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
