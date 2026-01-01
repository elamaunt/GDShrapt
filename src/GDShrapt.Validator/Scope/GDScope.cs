using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// A single lexical scope containing declared symbols.
    /// Scopes form a linked list via Parent for hierarchical lookup.
    /// </summary>
    public class GDScope
    {
        private readonly Dictionary<string, GDSymbol> _symbols;

        public GDScopeType Type { get; }
        public GDScope Parent { get; }
        public IEnumerable<GDSymbol> Symbols => _symbols.Values;
        public GDNode Node { get; }

        public GDScope(GDScopeType type, GDScope parent = null, GDNode node = null)
        {
            Type = type;
            Parent = parent;
            Node = node;
            _symbols = new Dictionary<string, GDSymbol>();
        }

        /// <summary>
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
        /// Overwrites if exists.
        /// </summary>
        public void Declare(GDSymbol symbol)
        {
            _symbols[symbol.Name] = symbol;
        }

        /// <summary>
        /// This scope only.
        /// </summary>
        public GDSymbol LookupLocal(string name)
        {
            _symbols.TryGetValue(name, out var symbol);
            return symbol;
        }

        /// <summary>
        /// This scope + all parents.
        /// </summary>
        public GDSymbol Lookup(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
                return symbol;

            return Parent?.Lookup(name);
        }

        public bool ContainsLocal(string name)
        {
            return _symbols.ContainsKey(name);
        }

        public bool Contains(string name)
        {
            return _symbols.ContainsKey(name) || (Parent?.Contains(name) ?? false);
        }

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
}
