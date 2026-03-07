using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry mapping opaque IDs to AST nodes and tokens.
/// Used by GDSemanticModel to bridge between handle-based public API and internal AST.
/// </summary>
internal class GDNodeRegistry
{
    private int _nextNodeId = 1;
    private int _nextTokenId = 1;
    private readonly Dictionary<int, GDNode> _nodes = new();
    private readonly Dictionary<int, GDSyntaxToken> _tokens = new();

    /// <summary>
    /// Registers an AST node and returns a handle with position info + opaque ID.
    /// </summary>
    public GDNodeHandle Register(GDNode? node, string? filePath = null)
    {
        if (node == null)
            return GDNodeHandle.Empty;

        var id = _nextNodeId++;
        _nodes[id] = node;

        return new GDNodeHandle(node.StartLine, node.StartColumn, node.EndLine, node.EndColumn, filePath, id);
    }

    /// <summary>
    /// Registers a syntax token and returns a handle with position info + opaque ID.
    /// </summary>
    public GDTokenHandle RegisterToken(GDSyntaxToken? token)
    {
        if (token == null)
            return GDTokenHandle.Empty;

        var id = _nextTokenId++;
        _tokens[id] = token;

        return new GDTokenHandle(
            token.StartLine,
            token.StartColumn,
            token.StartLine,
            token.StartColumn + (token.Length > 0 ? token.Length : 0),
            token.ToString(),
            id);
    }

    /// <summary>
    /// Resolves a handle back to the original AST node. Returns null if not found.
    /// </summary>
    public GDNode? ResolveNode(GDNodeHandle handle)
    {
        if (handle.IsEmpty)
            return null;

        return _nodes.TryGetValue(handle.NodeId, out var node) ? node : null;
    }

    /// <summary>
    /// Resolves a handle back to the original syntax token. Returns null if not found.
    /// </summary>
    public GDSyntaxToken? ResolveToken(GDTokenHandle handle)
    {
        if (handle.IsEmpty)
            return null;

        return _tokens.TryGetValue(handle.TokenId, out var token) ? token : null;
    }

    /// <summary>
    /// Clears all registered nodes and tokens.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _tokens.Clear();
        _nextNodeId = 1;
        _nextTokenId = 1;
    }

    internal static GDSyntaxToken? FindFirstToken(GDNode node)
    {
        return node.FirstLeafToken;
    }

    internal static GDSyntaxToken? FindLastToken(GDNode node)
    {
        return node.LastLeafToken;
    }
}

/// <summary>
/// Extension methods for converting Reader AST nodes to handle types.
/// For use by internal AST-walking code (Validator, Semantics).
/// </summary>
public static class GDNodeHandleExtensions
{
    /// <summary>
    /// Creates a position-only GDNodeHandle from an AST node (no registry, NodeId = 0).
    /// </summary>
    public static GDNodeHandle ToHandle(this GDNode? node, string? filePath = null)
    {
        if (node == null)
            return GDNodeHandle.Empty;

        return new GDNodeHandle(node.StartLine, node.StartColumn, node.EndLine, node.EndColumn, filePath, 0);
    }

    /// <summary>
    /// Creates a position-only GDTokenHandle from a syntax token (no registry, TokenId = 0).
    /// </summary>
    public static GDTokenHandle ToHandle(this GDSyntaxToken? token)
    {
        if (token == null)
            return GDTokenHandle.Empty;

        return new GDTokenHandle(
            token.StartLine,
            token.StartColumn,
            token.StartLine,
            token.StartColumn + (token.Length > 0 ? token.Length : 0),
            token.ToString(),
            0);
    }
}
