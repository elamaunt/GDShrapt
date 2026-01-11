using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when the same resource is loaded multiple times with load() or preload().
    /// Duplicated loads waste memory and performance.
    /// Compatible with gdtoolkit's duplicated-load rule.
    /// </summary>
    public class GDDuplicatedLoadRule : GDLintRule
    {
        public override string RuleId => "GDL219";
        public override string Name => "duplicated-load";
        public override string Description => "Duplicated load()/preload() calls with the same path";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        private HashSet<string> _loadedPaths;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            // Reset for each class
            _loadedPaths = new HashSet<string>();
            base.Visit(classDeclaration);
        }

        public override void Visit(GDCallExpression call)
        {
            if (_loadedPaths == null)
                _loadedPaths = new HashSet<string>();

            // Check if this is a load() or preload() call
            if (IsLoadOrPreloadCall(call, out string path))
            {
                if (!string.IsNullOrEmpty(path))
                {
                    if (!_loadedPaths.Add(path))
                    {
                        // Path was already loaded
                        ReportIssue(
                            $"Duplicated load: '{path}' is already loaded elsewhere in this file",
                            call.CallerExpression,
                            "Store the loaded resource in a variable and reuse it");
                    }
                }
            }

            base.Visit(call);
        }

        private bool IsLoadOrPreloadCall(GDCallExpression call, out string path)
        {
            path = null;

            // Check if the caller is 'load' or 'preload'
            if (call.CallerExpression is GDIdentifierExpression idExpr)
            {
                var funcName = idExpr.Identifier?.Sequence;
                if (funcName == "load" || funcName == "preload")
                {
                    // Get the first parameter (the path)
                    if (call.Parameters != null)
                    {
                        foreach (var param in call.Parameters)
                        {
                            if (param is GDStringExpression strExpr)
                            {
                                path = strExpr.String?.Sequence;
                                return true;
                            }
                            break; // Only check first parameter
                        }
                    }
                }
            }

            return false;
        }
    }
}
