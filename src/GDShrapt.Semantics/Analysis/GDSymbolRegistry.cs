using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry for symbols, their declarations, and references.
/// Extracted from GDSemanticModel to provide cleaner separation of concerns.
/// </summary>
public class GDSymbolRegistry
{
    // Symbol tracking
    private readonly Dictionary<GDNode, GDSymbolInfo> _nodeToSymbol = new();
    private readonly Dictionary<string, List<GDSymbolInfo>> _nameToSymbols = new();
    private readonly Dictionary<GDSymbolInfo, List<GDReference>> _symbolReferences = new();

    // Member access index: (CallerType, MemberName) -> references
    private readonly Dictionary<(string CallerType, string MemberName), List<GDReference>> _memberAccessByType =
        new(MemberAccessKeyComparer.Instance);

    /// <summary>
    /// All symbols in this registry.
    /// </summary>
    public IEnumerable<GDSymbolInfo> Symbols => _nameToSymbols.Values.SelectMany(x => x);

    /// <summary>
    /// Gets the symbol declared at a specific node.
    /// </summary>
    public GDSymbolInfo? GetSymbolForNode(GDNode node)
    {
        return _nodeToSymbol.TryGetValue(node, out var symbol) ? symbol : null;
    }

    /// <summary>
    /// Finds a symbol by name.
    /// </summary>
    public GDSymbolInfo? FindSymbol(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return _nameToSymbols.TryGetValue(name, out var symbols) && symbols.Count > 0
            ? symbols[0]
            : null;
    }

    /// <summary>
    /// Finds all symbols with the given name.
    /// </summary>
    public IEnumerable<GDSymbolInfo> FindSymbols(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Enumerable.Empty<GDSymbolInfo>();
        return _nameToSymbols.TryGetValue(name, out var symbols)
            ? symbols
            : Enumerable.Empty<GDSymbolInfo>();
    }

    /// <summary>
    /// Gets all references to a symbol.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferences(GDSymbolInfo symbol)
    {
        return _symbolReferences.TryGetValue(symbol, out var refs)
            ? refs
            : Array.Empty<GDReference>();
    }

    /// <summary>
    /// Gets all references to a member on a specific type.
    /// </summary>
    public IReadOnlyList<GDReference> GetMemberAccessReferences(string callerType, string memberName)
    {
        var key = (callerType, memberName);
        return _memberAccessByType.TryGetValue(key, out var refs)
            ? refs
            : Array.Empty<GDReference>();
    }

    /// <summary>
    /// Gets all symbols of a specific kind.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetSymbolsOfKind(GDSymbolKind kind)
    {
        return Symbols.Where(s => s.Kind == kind);
    }

    /// <summary>
    /// Gets all methods.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetMethods() => GetSymbolsOfKind(GDSymbolKind.Method);

    /// <summary>
    /// Gets all variables.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetVariables() => GetSymbolsOfKind(GDSymbolKind.Variable);

    /// <summary>
    /// Gets all signals.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetSignals() => GetSymbolsOfKind(GDSymbolKind.Signal);

    /// <summary>
    /// Gets all constants.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetConstants() => GetSymbolsOfKind(GDSymbolKind.Constant);

    // ========================================
    // Registration Methods (internal)
    // ========================================

    /// <summary>
    /// Registers a symbol.
    /// </summary>
    internal void RegisterSymbol(GDSymbolInfo symbol)
    {
        if (symbol == null || string.IsNullOrEmpty(symbol.Name))
            return;

        if (!_nameToSymbols.TryGetValue(symbol.Name, out var list))
        {
            list = new List<GDSymbolInfo>();
            _nameToSymbols[symbol.Name] = list;
        }
        list.Add(symbol);

        if (symbol.DeclarationNode != null)
        {
            _nodeToSymbol[symbol.DeclarationNode] = symbol;
        }
    }

    /// <summary>
    /// Registers a node as belonging to a symbol.
    /// </summary>
    internal void RegisterNodeSymbol(GDNode node, GDSymbolInfo symbol)
    {
        if (node != null && symbol != null)
        {
            _nodeToSymbol[node] = symbol;
        }
    }

    /// <summary>
    /// Registers a reference to a symbol.
    /// </summary>
    internal void RegisterReference(GDSymbolInfo symbol, GDReference reference)
    {
        if (symbol == null || reference == null)
            return;

        if (!_symbolReferences.TryGetValue(symbol, out var refs))
        {
            refs = new List<GDReference>();
            _symbolReferences[symbol] = refs;
        }
        refs.Add(reference);
    }

    /// <summary>
    /// Registers a member access reference.
    /// </summary>
    internal void RegisterMemberAccess(string callerType, string memberName, GDReference reference)
    {
        if (string.IsNullOrEmpty(callerType) || string.IsNullOrEmpty(memberName) || reference == null)
            return;

        var key = (callerType, memberName);
        if (!_memberAccessByType.TryGetValue(key, out var refs))
        {
            refs = new List<GDReference>();
            _memberAccessByType[key] = refs;
        }
        refs.Add(reference);
    }

    /// <summary>
    /// Clears all data in the registry.
    /// </summary>
    internal void Clear()
    {
        _nodeToSymbol.Clear();
        _nameToSymbols.Clear();
        _symbolReferences.Clear();
        _memberAccessByType.Clear();
    }
}
