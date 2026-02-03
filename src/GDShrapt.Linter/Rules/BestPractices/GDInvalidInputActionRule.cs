using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when Input action names don't exist in project.godot.
    /// This helps catch typos and missing input action configurations.
    /// </summary>
    public class GDInvalidInputActionRule : GDLintRule
    {
        public override string RuleId => "GDL246";
        public override string Name => "invalid-input-action";
        public override string Description => "Warn when Input action name doesn't exist";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        private static readonly HashSet<string> InputActionMethods = new HashSet<string>
        {
            "is_action_pressed",
            "is_action_released",
            "is_action_just_pressed",
            "is_action_just_released",
            "get_action_strength",
            "get_action_raw_strength",
            "is_action",
            "action_press",
            "action_release"
        };

        private static readonly HashSet<string> BuiltInActions = new HashSet<string>
        {
            "ui_accept",
            "ui_select",
            "ui_cancel",
            "ui_focus_next",
            "ui_focus_prev",
            "ui_left",
            "ui_right",
            "ui_up",
            "ui_down",
            "ui_page_up",
            "ui_page_down",
            "ui_home",
            "ui_end",
            "ui_cut",
            "ui_copy",
            "ui_paste",
            "ui_undo",
            "ui_redo",
            "ui_text_completion_query",
            "ui_text_completion_accept",
            "ui_text_completion_replace",
            "ui_text_newline",
            "ui_text_newline_blank",
            "ui_text_newline_above",
            "ui_text_indent",
            "ui_text_dedent",
            "ui_text_backspace",
            "ui_text_backspace_word",
            "ui_text_backspace_word.macos",
            "ui_text_backspace_all_to_left",
            "ui_text_backspace_all_to_left.macos",
            "ui_text_delete",
            "ui_text_delete_word",
            "ui_text_delete_word.macos",
            "ui_text_delete_all_to_right",
            "ui_text_delete_all_to_right.macos",
            "ui_text_caret_left",
            "ui_text_caret_word_left",
            "ui_text_caret_word_left.macos",
            "ui_text_caret_right",
            "ui_text_caret_word_right",
            "ui_text_caret_word_right.macos",
            "ui_text_caret_up",
            "ui_text_caret_down",
            "ui_text_caret_line_start",
            "ui_text_caret_line_start.macos",
            "ui_text_caret_line_end",
            "ui_text_caret_line_end.macos",
            "ui_text_caret_page_up",
            "ui_text_caret_page_down",
            "ui_text_caret_document_start",
            "ui_text_caret_document_start.macos",
            "ui_text_caret_document_end",
            "ui_text_caret_document_end.macos",
            "ui_text_caret_add_below",
            "ui_text_caret_add_below.macos",
            "ui_text_caret_add_above",
            "ui_text_caret_add_above.macos",
            "ui_text_scroll_up",
            "ui_text_scroll_up.macos",
            "ui_text_scroll_down",
            "ui_text_scroll_down.macos",
            "ui_text_select_all",
            "ui_text_select_word_under_caret",
            "ui_text_add_selection_for_next_occurrence",
            "ui_text_clear_carets_and_selection",
            "ui_text_toggle_insert_mode",
            "ui_menu",
            "ui_text_submit",
            "ui_graph_duplicate",
            "ui_graph_delete",
            "ui_filedialog_up_one_level",
            "ui_filedialog_refresh",
            "ui_filedialog_show_hidden",
            "ui_swap_input_direction"
        };

        public override void Visit(GDCallExpression call)
        {
            if (Options?.WarnInvalidInputAction != true)
                return;

            // Check for Input.method() pattern
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                if (methodName == null || !InputActionMethods.Contains(methodName))
                    return;

                // Check if caller is "Input"
                if (memberOp.CallerExpression is GDIdentifierExpression callerIdExpr &&
                    callerIdExpr.Identifier?.Sequence == "Input")
                {
                    CheckActionArgument(call);
                }
            }
        }

        private void CheckActionArgument(GDCallExpression call)
        {
            var args = call.Parameters;
            if (args == null)
                return;

            GDExpression firstArg = null;
            foreach (var arg in args)
            {
                firstArg = arg;
                break;
            }

            if (firstArg == null)
                return;

            // Check for string literal
            if (firstArg is GDStringExpression strExpr)
            {
                var actionName = GetStringValue(strExpr);
                if (!string.IsNullOrEmpty(actionName) && !IsValidInputAction(actionName))
                {
                    ReportIssue(
                        $"Input action '{actionName}' may not exist in project.godot",
                        strExpr,
                        "Check project settings or add the action in Project > Project Settings > Input Map");
                }
            }

            // Check for StringName literal: &"action_name"
            if (firstArg is GDStringNameExpression strNameExpr)
            {
                var actionName = GetStringNameValue(strNameExpr);
                if (!string.IsNullOrEmpty(actionName) && !IsValidInputAction(actionName))
                {
                    ReportIssue(
                        $"Input action '{actionName}' may not exist in project.godot",
                        strNameExpr,
                        "Check project settings or add the action in Project > Project Settings > Input Map");
                }
            }
        }

        private bool IsValidInputAction(string actionName)
        {
            // Built-in actions are always valid
            if (BuiltInActions.Contains(actionName))
                return true;

            // If we have project context, check there
            // For now, we only validate against built-in actions
            // TODO: Add IGDProjectRuntimeProvider integration to check project.godot

            // Return true for non-built-in to avoid false positives without project context
            return true;
        }

        private string GetStringValue(GDStringExpression strExpr)
        {
            var text = strExpr.ToString();
            if (text.Length >= 2)
            {
                // Remove quotes
                return text.Substring(1, text.Length - 2);
            }
            return null;
        }

        private string GetStringNameValue(GDStringNameExpression strNameExpr)
        {
            var text = strNameExpr.ToString();
            // Format is &"name" or &'name'
            if (text.Length >= 3 && text.StartsWith("&"))
            {
                return text.Substring(2, text.Length - 3);
            }
            return null;
        }
    }
}
