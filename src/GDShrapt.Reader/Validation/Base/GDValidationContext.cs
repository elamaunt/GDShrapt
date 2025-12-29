using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Shared state for all validators: scope stack, diagnostics collection, and runtime provider.
    /// </summary>
    public class GDValidationContext
    {
        private readonly List<GDDiagnostic> _diagnostics;

        public GDScopeStack Scopes { get; }
        public IReadOnlyList<GDDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// The runtime provider for type information.
        /// </summary>
        public IGDRuntimeProvider RuntimeProvider { get; }

        public bool IsInLoop => Scopes.IsInLoop;
        public bool IsInFunction => Scopes.IsInFunction;
        public bool IsInClass => Scopes.IsInClass;

        public GDValidationContext() : this(null)
        {
        }

        public GDValidationContext(IGDRuntimeProvider runtimeProvider)
        {
            _diagnostics = new List<GDDiagnostic>();
            Scopes = new GDScopeStack();
            RuntimeProvider = runtimeProvider ?? GDDefaultRuntimeProvider.Instance;
        }

        public void AddError(GDDiagnosticCode code, string message, GDNode node)
        {
            _diagnostics.Add(GDDiagnostic.Error(code, message, node));
        }

        public void AddError(GDDiagnosticCode code, string message, GDSyntaxToken token)
        {
            _diagnostics.Add(GDDiagnostic.Error(code, message, token));
        }

        public void AddWarning(GDDiagnosticCode code, string message, GDNode node)
        {
            _diagnostics.Add(GDDiagnostic.Warning(code, message, node));
        }

        public void AddWarning(GDDiagnosticCode code, string message, GDSyntaxToken token)
        {
            _diagnostics.Add(GDDiagnostic.Warning(code, message, token));
        }

        public void AddHint(GDDiagnosticCode code, string message, GDNode node)
        {
            _diagnostics.Add(GDDiagnostic.Hint(code, message, node));
        }

        public void AddHint(GDDiagnosticCode code, string message, GDSyntaxToken token)
        {
            _diagnostics.Add(GDDiagnostic.Hint(code, message, token));
        }

        public GDScope EnterScope(GDScopeType type, GDNode node = null)
        {
            return Scopes.Push(type, node);
        }

        public GDScope ExitScope()
        {
            return Scopes.Pop();
        }

        /// <summary>
        /// Returns false if symbol already exists in current scope.
        /// </summary>
        public bool TryDeclare(GDSymbol symbol)
        {
            return Scopes.TryDeclare(symbol);
        }

        public void Declare(GDSymbol symbol)
        {
            Scopes.Declare(symbol);
        }

        /// <summary>
        /// Searches all scopes from innermost to outermost.
        /// </summary>
        public GDSymbol Lookup(string name)
        {
            return Scopes.Lookup(name);
        }

        public bool Contains(string name)
        {
            return Scopes.Contains(name);
        }

        public GDValidationResult BuildResult()
        {
            return new GDValidationResult(_diagnostics);
        }
    }
}
