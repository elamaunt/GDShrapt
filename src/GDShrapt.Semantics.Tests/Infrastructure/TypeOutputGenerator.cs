using System.Text;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Generates TYPE_INFERENCE_OUTPUT.txt from collected type nodes.
/// </summary>
public class TypeOutputGenerator
{
    private readonly TypeNodeCollector _collector = new();

    /// <summary>
    /// Generates the output file with all type inferences from the project.
    /// </summary>
    public void GenerateOutput(GDScriptProject project, string outputPath, string projectBasePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TYPE_INFERENCE_OUTPUT.txt");
        sb.AppendLine("# Auto-generated - do not edit manually");
        sb.AppendLine("# Format: LINE:COL NODEKIND NAME -> TYPE");
        sb.AppendLine();

        int totalNodes = 0;
        int totalFiles = 0;

        foreach (var file in project.ScriptFiles.OrderBy(f => f.FullPath))
        {
            if (file.FullPath == null)
                continue;

            var nodes = _collector.CollectNodes(file);
            if (nodes.Count == 0)
                continue;

            totalFiles++;
            totalNodes += nodes.Count;

            // Write file header with relative path
            var relativePath = GetRelativePath(file.FullPath, projectBasePath);
            sb.AppendLine(relativePath);

            foreach (var node in nodes)
            {
                sb.AppendLine(FormatNode(node));
            }

            sb.AppendLine();
        }

        // Write summary at the end
        sb.AppendLine($"# Summary: {totalNodes} nodes in {totalFiles} files");

        File.WriteAllText(outputPath, sb.ToString());
    }

    /// <summary>
    /// Formats a single node entry.
    /// Format: LINE:COL NODEKIND NAME -> TYPE
    /// </summary>
    public string FormatNode(TypeNodeCollector.TypedNode node)
    {
        return $"{node.Line}:{node.Column} {node.NodeKind} {node.Name} -> {node.InferredType}";
    }

    private string GetRelativePath(string? fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        // Normalize paths to forward slashes and ensure trailing slash on basePath
        fullPath = fullPath.Replace('\\', '/');
        basePath = basePath.Replace('\\', '/');
        if (!basePath.EndsWith("/"))
            basePath += "/";

        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(basePath.Length);
        }

        return fullPath;
    }
}
