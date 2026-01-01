namespace GDShrapt.Reader
{
    /// <summary>
    /// Base class for validators. Provides helpers for reporting diagnostics and managing scopes.
    /// Use node.WalkIn(this) for traversal; override Visit/Left for entering/exiting nodes.
    /// </summary>
    public abstract class GDValidationVisitor : GDVisitor
    {
        protected GDValidationContext Context { get; }

        protected GDValidationVisitor(GDValidationContext context)
        {
            Context = context;
        }

        protected void ReportError(GDDiagnosticCode code, string message, GDNode node)
        {
            Context.AddError(code, message, node);
        }

        protected void ReportWarning(GDDiagnosticCode code, string message, GDNode node)
        {
            Context.AddWarning(code, message, node);
        }

        protected void ReportHint(GDDiagnosticCode code, string message, GDNode node)
        {
            Context.AddHint(code, message, node);
        }

        protected GDScope EnterScope(GDScopeType type, GDNode node = null)
        {
            return Context.EnterScope(type, node);
        }

        protected GDScope ExitScope()
        {
            return Context.ExitScope();
        }

        /// <summary>
        /// Declares symbol; reports DuplicateDeclaration error if already exists.
        /// </summary>
        protected bool TryDeclareSymbol(GDSymbol symbol)
        {
            if (!Context.TryDeclare(symbol))
            {
                var existing = Context.Scopes.LookupLocal(symbol.Name);
                ReportError(
                    GDDiagnosticCode.DuplicateDeclaration,
                    $"'{symbol.Name}' is already declared in this scope",
                    symbol.Declaration);
                return false;
            }
            return true;
        }

        protected GDSymbol LookupSymbol(string name)
        {
            return Context.Lookup(name);
        }

        protected bool SymbolExists(string name)
        {
            return Context.Contains(name);
        }
    }
}
