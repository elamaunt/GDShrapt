using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// A single lexical scope containing declared symbols.
/// Scopes form a linked list via Parent for hierarchical lookup.
/// </summary>
public class GDScope
{
    private readonly Dictionary<string, GDSymbol> _symbols;

    /// <summary>
    /// The type of this scope.
    /// </summary>
    public GDScopeType Type { get; }

    /// <summary>
    /// The parent scope, or null if this is the root scope.
    /// </summary>
    public GDScope? Parent { get; }

    /// <summary>
    /// All symbols declared in this scope (not including parent scopes).
    /// </summary>
    public IEnumerable<GDSymbol> Symbols => _symbols.Values;

    /// <summary>
    /// The AST node associated with this scope (e.g., method declaration, for statement).
    /// </summary>
    public GDNode? Node { get; }

    public GDScope(GDScopeType type, GDScope? parent = null, GDNode? node = null)
    {
        Type = type;
        Parent = parent;
        Node = node;
        _symbols = new Dictionary<string, GDSymbol>();
    }

    /// <summary>
    /// Tries to declare a symbol in this scope.
    /// Returns false if already declared in this scope.
    /// </summary>
    public bool TryDeclare(GDSymbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
            return false;

        _symbols[symbol.Name] = symbol;
        return true;
    }

    /// <summary>
    /// Declares a symbol in this scope, overwriting if exists.
    /// </summary>
    public void Declare(GDSymbol symbol)
    {
        _symbols[symbol.Name] = symbol;
    }

    /// <summary>
    /// Looks up a symbol in this scope only (not parent scopes).
    /// </summary>
    public GDSymbol? LookupLocal(string name)
    {
        _symbols.TryGetValue(name, out var symbol);
        return symbol;
    }

    /// <summary>
    /// Looks up a symbol in this scope and all parent scopes.
    /// </summary>
    public GDSymbol? Lookup(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;

        return Parent?.Lookup(name);
    }

    /// <summary>
    /// Checks if a symbol is declared in this scope only.
    /// </summary>
    public bool ContainsLocal(string name)
    {
        return _symbols.ContainsKey(name);
    }

    /// <summary>
    /// Checks if a symbol is declared in this scope or any parent scope.
    /// </summary>
    public bool Contains(string name)
    {
        return _symbols.ContainsKey(name) || (Parent?.Contains(name) ?? false);
    }

    /// <summary>
    /// Gets all symbols of a specific kind from this scope and parent scopes.
    /// </summary>
    public IEnumerable<GDSymbol> GetSymbolsOfKind(GDSymbolKind kind)
    {
        foreach (var symbol in _symbols.Values)
        {
            if (symbol.Kind == kind)
                yield return symbol;
        }

        if (Parent != null)
        {
            foreach (var symbol in Parent.GetSymbolsOfKind(kind))
                yield return symbol;
        }
    }
}
