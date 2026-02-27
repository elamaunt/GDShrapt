using System.Collections.Generic;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

public interface IGDFoldingRangeHandler
{
    IReadOnlyList<GDFoldingRegion> GetFoldingRegions(string filePath);
}

public class GDFoldingRegion
{
    public int StartLine { get; init; }
    public int? StartColumn { get; init; }
    public int EndLine { get; init; }
    public int? EndColumn { get; init; }
    public string? Kind { get; init; }
}

public class GDFoldingRangeHandler : IGDFoldingRangeHandler
{
    private readonly GDScriptProject _project;

    public GDFoldingRangeHandler(GDScriptProject project)
    {
        _project = project;
    }

    public IReadOnlyList<GDFoldingRegion> GetFoldingRegions(string filePath)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null)
            return [];

        var regions = new List<GDFoldingRegion>();
        CollectFoldingRegions(script.Class, regions);
        CollectCommentRegions(script.Class, regions);
        return regions;
    }

    private void CollectFoldingRegions(GDNode node, List<GDFoldingRegion> regions)
    {
        if (IsFoldableNode(node) && node.EndLine > node.StartLine)
        {
            regions.Add(new GDFoldingRegion
            {
                StartLine = node.StartLine,
                EndLine = node.EndLine
            });
        }

        foreach (var child in node.Nodes)
        {
            CollectFoldingRegions(child, regions);
        }
    }

    private static bool IsFoldableNode(GDNode node)
    {
        return node is GDMethodDeclaration
            or GDInnerClassDeclaration
            or GDIfStatement
            or GDForStatement
            or GDWhileStatement
            or GDMatchStatement
            or GDEnumDeclaration;
    }

    private static void CollectCommentRegions(GDNode root, List<GDFoldingRegion> regions)
    {
        int? groupStartLine = null;
        int lastCommentLine = -2;

        foreach (var token in root.AllTokens)
        {
            if (token is GDComment)
            {
                var line = token.StartLine;
                if (groupStartLine == null)
                {
                    groupStartLine = line;
                    lastCommentLine = line;
                }
                else if (line == lastCommentLine + 1)
                {
                    lastCommentLine = line;
                }
                else
                {
                    if (lastCommentLine > groupStartLine.Value)
                    {
                        regions.Add(new GDFoldingRegion
                        {
                            StartLine = groupStartLine.Value,
                            EndLine = lastCommentLine,
                            Kind = "comment"
                        });
                    }
                    groupStartLine = line;
                    lastCommentLine = line;
                }
            }
        }

        if (groupStartLine != null && lastCommentLine > groupStartLine.Value)
        {
            regions.Add(new GDFoldingRegion
            {
                StartLine = groupStartLine.Value,
                EndLine = lastCommentLine,
                Kind = "comment"
            });
        }
    }
}
