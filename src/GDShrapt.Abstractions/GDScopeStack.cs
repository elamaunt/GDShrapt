using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Stack of nested scopes. Tracks current context (in loop, in function, etc).
/// </summary>
public class GDScopeStack
{
    private readonly Stack<GDScope> _stack;

    /// <summary>
    /// The current innermost scope.
    /// </summary>
    public GDScope? Current => _stack.Count > 0 ? _stack.Peek() : null;

    /// <summary>
    /// The global (file-level) scope.
    /// </summary>
    public GDScope? Global { get; private set; }

    /// <summary>
    /// Current nesting depth.
    /// </summary>
    public int Depth => _stack.Count;

    /// <summary>
    /// True if currently inside a loop (for or while).
    /// </summary>
    public bool IsInLoop => _stack.Any(s => s.Type == GDScopeType.ForLoop || s.Type == GDScopeType.WhileLoop);

    /// <summary>
    /// True if currently inside a function or lambda.
    /// </summary>
    public bool IsInFunction => _stack.Any(s => s.Type == GDScopeType.Method || s.Type == GDScopeType.Lambda);

    /// <summary>
    /// True if currently inside a class.
    /// </summary>
    public bool IsInClass => _stack.Any(s => s.Type == GDScopeType.Class);

    public GDScopeStack()
    {
        _stack = new Stack<GDScope>();
    }

    /// <summary>
    /// Pushes a new scope onto the stack.
    /// </summary>
    public GDScope Push(GDScopeType type, GDNode? node = null)
    {
        var parent = Current;
        var scope = new GDScope(type, parent, node);

        if (type == GDScopeType.Global)
            Global = scope;

        _stack.Push(scope);
        return scope;
    }

    /// <summary>
    /// Pops the current scope from the stack.
    /// </summary>
    public GDScope? Pop()
    {
        if (_stack.Count > 0)
            return _stack.Pop();
        return null;
    }

    /// <summary>
    /// Tries to declare a symbol in the current scope.
    /// </summary>
    public bool TryDeclare(GDSymbol symbol)
    {
        return Current?.TryDeclare(symbol) ?? false;
    }

    /// <summary>
    /// Declares a symbol in the current scope.
    /// </summary>
    public void Declare(GDSymbol symbol)
    {
        Current?.Declare(symbol);
    }

    /// <summary>
    /// Searches from current scope up through parents.
    /// </summary>
    public GDSymbol? Lookup(string name)
    {
        return Current?.Lookup(name);
    }

    /// <summary>
    /// Searches only in the current scope.
    /// </summary>
    public GDSymbol? LookupLocal(string name)
    {
        return Current?.LookupLocal(name);
    }

    /// <summary>
    /// Checks if a symbol is visible from the current scope.
    /// </summary>
    public bool Contains(string name)
    {
        return Current?.Contains(name) ?? false;
    }

    /// <summary>
    /// Finds nearest enclosing scope of given type.
    /// </summary>
    public GDScope? GetEnclosingScope(GDScopeType type)
    {
        foreach (var scope in _stack)
        {
            if (scope.Type == type)
                return scope;
        }
        return null;
    }

    /// <summary>
    /// Gets the nearest enclosing loop scope.
    /// </summary>
    public GDScope? GetEnclosingLoop()
    {
        foreach (var scope in _stack)
        {
            if (scope.Type == GDScopeType.ForLoop || scope.Type == GDScopeType.WhileLoop)
                return scope;
        }
        return null;
    }

    /// <summary>
    /// Gets the nearest enclosing function or lambda scope.
    /// </summary>
    public GDScope? GetEnclosingFunction()
    {
        foreach (var scope in _stack)
        {
            if (scope.Type == GDScopeType.Method || scope.Type == GDScopeType.Lambda)
                return scope;
        }
        return null;
    }

    /// <summary>
    /// Gets all symbols of a specific kind from the current scope chain.
    /// </summary>
    public IEnumerable<GDSymbol> GetSymbolsOfKind(GDSymbolKind kind)
    {
        return Current?.GetSymbolsOfKind(kind) ?? Enumerable.Empty<GDSymbol>();
    }

    /// <summary>
    /// Resets stack to only contain the Global scope (keeping its symbols).
    /// Used for two-pass validation.
    /// </summary>
    public void ResetToGlobal()
    {
        while (_stack.Count > 0)
        {
            var scope = _stack.Peek();
            if (scope == Global)
                break;
            _stack.Pop();
        }
    }
}
