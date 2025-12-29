using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Stack of nested scopes. Tracks current context (in loop, in function, etc).
    /// </summary>
    public class GDScopeStack
    {
        private readonly Stack<GDScope> _stack;

        public GDScope Current => _stack.Count > 0 ? _stack.Peek() : null;
        public GDScope Global { get; private set; }
        public int Depth => _stack.Count;

        public bool IsInLoop => _stack.Any(s => s.Type == GDScopeType.ForLoop || s.Type == GDScopeType.WhileLoop);
        public bool IsInFunction => _stack.Any(s => s.Type == GDScopeType.Method || s.Type == GDScopeType.Lambda);
        public bool IsInClass => _stack.Any(s => s.Type == GDScopeType.Class);

        public GDScopeStack()
        {
            _stack = new Stack<GDScope>();
        }

        public GDScope Push(GDScopeType type, GDNode node = null)
        {
            var parent = Current;
            var scope = new GDScope(type, parent, node);

            if (type == GDScopeType.Global)
                Global = scope;

            _stack.Push(scope);
            return scope;
        }

        public GDScope Pop()
        {
            if (_stack.Count > 0)
                return _stack.Pop();
            return null;
        }

        public bool TryDeclare(GDSymbol symbol)
        {
            return Current?.TryDeclare(symbol) ?? false;
        }

        public void Declare(GDSymbol symbol)
        {
            Current?.Declare(symbol);
        }

        /// <summary>
        /// Searches from current scope up through parents.
        /// </summary>
        public GDSymbol Lookup(string name)
        {
            return Current?.Lookup(name);
        }

        public GDSymbol LookupLocal(string name)
        {
            return Current?.LookupLocal(name);
        }

        public bool Contains(string name)
        {
            return Current?.Contains(name) ?? false;
        }

        /// <summary>
        /// Finds nearest enclosing scope of given type.
        /// </summary>
        public GDScope GetEnclosingScope(GDScopeType type)
        {
            foreach (var scope in _stack)
            {
                if (scope.Type == type)
                    return scope;
            }
            return null;
        }

        public GDScope GetEnclosingLoop()
        {
            foreach (var scope in _stack)
            {
                if (scope.Type == GDScopeType.ForLoop || scope.Type == GDScopeType.WhileLoop)
                    return scope;
            }
            return null;
        }

        public GDScope GetEnclosingFunction()
        {
            foreach (var scope in _stack)
            {
                if (scope.Type == GDScopeType.Method || scope.Type == GDScopeType.Lambda)
                    return scope;
            }
            return null;
        }

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
}
