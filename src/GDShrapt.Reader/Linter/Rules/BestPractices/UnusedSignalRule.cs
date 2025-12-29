using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks for signals that are declared but never emitted.
    /// Unused signals may indicate dead code or incomplete implementation.
    /// </summary>
    public class GDUnusedSignalRule : GDLintRule
    {
        public override string RuleId => "GDL207";
        public override string Name => "unused-signal";
        public override string Description => "Warn about signals that are declared but never emitted";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Info;
        public override bool EnabledByDefault => true;

        private readonly HashSet<string> _declaredSignals = new HashSet<string>();
        private readonly HashSet<string> _emittedSignals = new HashSet<string>();
        private readonly Dictionary<string, GDSignalDeclaration> _signalDeclarations = new Dictionary<string, GDSignalDeclaration>();
        private GDClassDeclaration _currentClass;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            if (Options?.WarnUnusedSignals != true)
                return;

            _currentClass = classDeclaration;
            _declaredSignals.Clear();
            _emittedSignals.Clear();
            _signalDeclarations.Clear();
        }

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            if (Options?.WarnUnusedSignals != true)
                return;

            var signalName = signalDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(signalName))
            {
                _declaredSignals.Add(signalName);
                _signalDeclarations[signalName] = signalDeclaration;
            }
        }

        public override void Visit(GDCallExpression callExpression)
        {
            if (Options?.WarnUnusedSignals != true)
                return;

            // Check for emit_signal("signal_name") or signal_name.emit()
            var callee = callExpression.CallerExpression;

            // Pattern 1: emit_signal("signal_name")
            if (callee is GDIdentifierExpression identExpr)
            {
                if (identExpr.Identifier?.Sequence == "emit_signal")
                {
                    // First argument is the signal name
                    var args = callExpression.Parameters?.ToList();
                    if (args?.Count > 0)
                    {
                        var firstArg = args[0];
                        if (firstArg is GDStringExpression stringExpr)
                        {
                            // Extract signal name from string
                            var signalName = ExtractStringValue(stringExpr);
                            if (!string.IsNullOrEmpty(signalName))
                            {
                                _emittedSignals.Add(signalName);
                            }
                        }
                    }
                }
            }

            // Pattern 2: signal_name.emit() - member access
            if (callee is GDMemberOperatorExpression memberExpr)
            {
                var memberName = memberExpr.Identifier?.Sequence;
                if (memberName == "emit")
                {
                    // The signal is the caller of the member
                    if (memberExpr.CallerExpression is GDIdentifierExpression signalIdent)
                    {
                        var signalName = signalIdent.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(signalName))
                        {
                            _emittedSignals.Add(signalName);
                        }
                    }
                }
            }
        }

        public override void Left(GDClassDeclaration classDeclaration)
        {
            if (Options?.WarnUnusedSignals != true)
                return;

            if (classDeclaration != _currentClass)
                return;

            // Report unused signals
            foreach (var signalName in _declaredSignals)
            {
                if (!_emittedSignals.Contains(signalName))
                {
                    if (_signalDeclarations.TryGetValue(signalName, out var decl))
                    {
                        ReportIssue(
                            $"Signal '{signalName}' is declared but never emitted",
                            decl.Identifier,
                            "Remove the signal or emit it when appropriate");
                    }
                }
            }

            _currentClass = null;
        }

        private string ExtractStringValue(GDStringExpression stringExpr)
        {
            // Get the string content through the String property
            var stringNode = stringExpr.String;
            if (stringNode?.Parts == null)
                return null;

            foreach (var part in stringNode.Parts)
            {
                if (part is GDStringPart stringPart)
                {
                    return stringPart.Sequence;
                }
            }

            return null;
        }
    }
}
