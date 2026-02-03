using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about private functions that are never called.
    /// Private functions (prefixed with _) that are not built-in callbacks
    /// and never called are likely dead code.
    /// </summary>
    public class GDUnusedFunctionRule : GDLintRule
    {
        public override string RuleId => "GDL252";
        public override string Name => "unused-function";
        public override string Description => "Warn about private functions that are never called";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        private static readonly HashSet<string> BuiltinCallbacks = new HashSet<string>
        {
            "_ready",
            "_process",
            "_physics_process",
            "_input",
            "_unhandled_input",
            "_unhandled_key_input",
            "_enter_tree",
            "_exit_tree",
            "_init",
            "_notification",
            "_draw",
            "_gui_input",
            "_get_property_list",
            "_set",
            "_get",
            "_get_configuration_warnings",
            "_validate_property",
            "_property_can_revert",
            "_property_get_revert",
            "_make_custom_tooltip",
            "_can_drop_data",
            "_drop_data",
            "_get_drag_data",
            "_get_minimum_size",
            "_has_point",
            "_structured_text_parser",
            "_run",
            "_editor_run",
            "_editor_run_node",
            "_get_tool_state",
            "_set_tool_state",
            "_forward_draw_over_viewport",
            "_forward_3d_draw_over_viewport",
            "_forward_canvas_draw_over_viewport",
            "_forward_3d_force_draw_over_viewport",
            "_forward_canvas_force_draw_over_viewport",
            "_handles",
            "_forward_3d_gui_input",
            "_forward_canvas_gui_input",
            "_edit",
            "_build",
            "_cleanup",
            "_get",
            "_get_import_options",
            "_get_option_visibility",
            "_get_preset_count",
            "_get_preset_name",
            "_get_recognized_extensions",
            "_get_import_order",
            "_get_priority",
            "_get_resource_type",
            "_import"
        };

        public override void Visit(GDClassDeclaration classDecl)
        {
            if (Options?.WarnUnusedFunctions != true)
                return;

            var privateFuncs = new Dictionary<string, GDMethodDeclaration>();
            var calledFuncs = new HashSet<string>();
            var connectedFuncs = new HashSet<string>();

            // Collect private functions
            foreach (var member in classDecl.Members ?? Enumerable.Empty<GDClassMember>())
            {
                if (member is GDMethodDeclaration method)
                {
                    var name = method.Identifier?.Sequence;
                    if (name?.StartsWith("_") == true && !IsBuiltinCallback(name))
                    {
                        privateFuncs[name] = method;
                    }
                }
            }

            // No private functions to check
            if (privateFuncs.Count == 0)
                return;

            // Collect all function calls and signal connections
            CollectCalls(classDecl, calledFuncs, connectedFuncs);

            // Report unused private functions
            foreach (var kvp in privateFuncs)
            {
                var funcName = kvp.Key;
                if (!calledFuncs.Contains(funcName) && !connectedFuncs.Contains(funcName))
                {
                    ReportIssue(
                        $"Private function '{funcName}' is never called",
                        kvp.Value.Identifier,
                        "Remove the function if it's not needed, or call it somewhere");
                }
            }
        }

        private void CollectCalls(GDClassDeclaration classDecl, HashSet<string> calledFuncs, HashSet<string> connectedFuncs)
        {
            foreach (var node in classDecl.AllNodes)
            {
                // Direct function calls
                if (node is GDCallExpression call)
                {
                    var name = GetCallMethodName(call);
                    if (!string.IsNullOrEmpty(name))
                    {
                        calledFuncs.Add(name);

                        // Check for signal.connect(callable) pattern
                        if (name == "connect" || name == "call_deferred" || name == "call")
                        {
                            CollectCallableArguments(call, connectedFuncs);
                        }
                    }
                }

                // Callable references: Callable(self, "_method_name")
                if (node is GDCallExpression callableExpr)
                {
                    var callableName = GetCallMethodName(callableExpr);
                    if (callableName == "Callable")
                    {
                        CollectCallableArguments(callableExpr, connectedFuncs);
                    }
                }
            }
        }

        private void CollectCallableArguments(GDCallExpression call, HashSet<string> connectedFuncs)
        {
            foreach (var param in call.Parameters ?? Enumerable.Empty<GDExpression>())
            {
                // String literal: connect("signal", self, "_on_signal")
                if (param is GDStringExpression strExpr)
                {
                    var value = GetStringValue(strExpr);
                    if (!string.IsNullOrEmpty(value) && value.StartsWith("_"))
                    {
                        connectedFuncs.Add(value);
                    }
                }

                // Callable: connect(Callable(self, "_on_signal"))
                if (param is GDCallExpression innerCall &&
                    GetCallMethodName(innerCall) == "Callable")
                {
                    CollectCallableArguments(innerCall, connectedFuncs);
                }

                // Method reference: signal.connect(_on_signal) in Godot 4
                if (param is GDIdentifierExpression idExpr)
                {
                    var name = idExpr.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                    {
                        connectedFuncs.Add(name);
                    }
                }
            }
        }

        private string GetCallMethodName(GDCallExpression call)
        {
            if (call.CallerExpression is GDIdentifierExpression idExpr)
                return idExpr.Identifier?.Sequence;

            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
                return memberOp.Identifier?.Sequence;

            return null;
        }

        private string GetStringValue(GDStringExpression strExpr)
        {
            var text = strExpr.ToString();
            if (text.Length >= 2)
            {
                return text.Substring(1, text.Length - 2);
            }
            return null;
        }

        private static bool IsBuiltinCallback(string name)
        {
            return BuiltinCallbacks.Contains(name);
        }
    }
}
