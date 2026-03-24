using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

public static class GDKeywordHoverProvider
{
    public static GDHoverInfo? GetKeywordHover(GDSyntaxToken token)
    {
        var (text, description) = token switch
        {
            GDVarKeyword => ("var", "Declares a variable."),
            GDConstKeyword => ("const", "Declares a constant."),
            GDFuncKeyword => ("func", "Declares a function."),
            GDSignalKeyword => ("signal", "Declares a signal."),
            GDClassKeyword => ("class", "Declares an inner class."),
            GDClassNameKeyword => ("class_name", "Sets the global class name for this script."),
            GDExtendsKeyword => ("extends", "Specifies the base class."),
            GDEnumKeyword => ("enum", "Declares an enumeration."),
            GDStaticKeyword => ("static", "Marks a member as static."),
            GDIfKeyword => ("if", "Conditional branch."),
            GDElifKeyword => ("elif", "Additional conditional branch."),
            GDElseKeyword => ("else", "Default branch when no conditions match."),
            GDForKeyword => ("for", "Iterates over a range or collection."),
            GDWhileKeyword => ("while", "Repeats while a condition is true."),
            GDMatchKeyword => ("match", "Pattern matching statement."),
            GDWhenKeyword => ("when", "Guard clause in match patterns."),
            GDReturnKeyword => ("return", "Returns a value from a function."),
            GDPassKeyword => ("pass", "Empty statement placeholder."),
            GDBreakKeyword => ("break", "Exits the current loop."),
            GDContinueKeyword => ("continue", "Skips to the next loop iteration."),
            GDAwaitKeyword => ("await", "Waits for a signal or coroutine to complete."),
            GDYieldKeyword => ("yield", "Pauses execution (GDScript 1.x)."),
            GDInKeyword => ("in", "Membership test or for-loop iterator keyword."),
            GDNotKeyword => ("not", "Logical negation operator."),
            GDExportKeyword => ("@export", "Exposes a variable to the Godot editor inspector."),
            GDOnreadyKeyword => ("@onready", "Initializes when the node enters the scene tree."),
            GDToolKeyword => ("@tool", "Makes the script run in the editor."),
            GDBreakPointKeyword => ("breakpoint", "Triggers a debugger breakpoint."),
            GDGetKeyword => ("get", "Property getter."),
            GDSetKeyword => ("set", "Property setter."),
            GDTrueKeyword => ("true", "Boolean true literal."),
            GDFalseKeyword => ("false", "Boolean false literal."),
            GDDualOperator { OperatorType: GDDualOperatorType.Or2 } => ("or", "Logical OR operator."),
            GDDualOperator { OperatorType: GDDualOperatorType.And2 } => ("and", "Logical AND operator."),
            GDDualOperator { OperatorType: GDDualOperatorType.As } => ("as", "Type cast operator."),
            GDDualOperator { OperatorType: GDDualOperatorType.Is } => ("is", "Type check operator."),
            GDDualOperator { OperatorType: GDDualOperatorType.In } => ("in", "Membership test operator."),
            _ => ((string?)null, (string?)null)
        };

        if (text == null || description == null)
        {
            // Check for attribute annotations (@onready, @export, etc.)
            // These are parsed as GDAttribute containing GDAt + GDIdentifier, not keyword tokens
            if (token is GDIdentifier id && token.Parent is GDAttribute attr && attr.Parent is GDCustomAttribute)
                return GetAttributeHover(id, attr);

            if (token is GDAt && token.Parent is GDAttribute parentAttr && parentAttr.Parent is GDCustomAttribute)
            {
                if (parentAttr.Name != null)
                    return GetAttributeHover(parentAttr.Name, parentAttr);
            }

            return null;
        }

        return new GDHoverInfo
        {
            Content = $"```gdscript\n(keyword) {text}\n```\n\n{description}",
            SymbolName = text,
            StartLine = token.StartLine + 1,
            StartColumn = token.StartColumn,
            EndLine = token.EndLine + 1,
            EndColumn = token.StartColumn + text.Length
        };
    }

    public static GDHoverInfo? GetAttributeHover(GDIdentifier? id, GDAttribute? attr)
    {
        if (id == null || attr == null)
            return null;

        var name = id.Sequence;
        var (text, description) = name switch
        {
            "onready" => ("@onready", "Initializes when the node enters the scene tree."),
            "export" => ("@export", "Exposes a variable to the Godot editor inspector."),
            "export_range" => ("@export_range", "Exports a numeric variable with min/max range."),
            "export_enum" => ("@export_enum", "Exports a string or int variable as an enum dropdown."),
            "export_file" => ("@export_file", "Exports a String variable as a file path."),
            "export_dir" => ("@export_dir", "Exports a String variable as a directory path."),
            "export_multiline" => ("@export_multiline", "Exports a String variable with a multiline editor."),
            "export_color_no_alpha" => ("@export_color_no_alpha", "Exports a Color without the alpha channel."),
            "export_node_path" => ("@export_node_path", "Exports a NodePath filtered to specific types."),
            "export_flags" => ("@export_flags", "Exports an integer as bit flags."),
            "export_exp_easing" => ("@export_exp_easing", "Exports a float with an easing curve editor."),
            "export_global_file" => ("@export_global_file", "Exports an absolute file path."),
            "export_global_dir" => ("@export_global_dir", "Exports an absolute directory path."),
            "export_placeholder" => ("@export_placeholder", "Exports a String with placeholder text."),
            "export_category" => ("@export_category", "Groups following exports into a category in inspector."),
            "export_group" => ("@export_group", "Groups following exports into a collapsible group in inspector."),
            "export_subgroup" => ("@export_subgroup", "Creates a subgroup within an export group."),
            "export_storage" => ("@export_storage", "Exports for serialization only, not shown in inspector."),
            "export_custom" => ("@export_custom", "Exports with custom property hints."),
            "icon" => ("@icon", "Sets a custom icon for this script's class."),
            "warning_ignore" => ("@warning_ignore", "Suppresses a specific GDScript warning."),
            "static_unload" => ("@static_unload", "Allows static variables to be freed on unload."),
            _ => ((string?)null, (string?)null)
        };

        if (text == null || description == null)
            return null;

        return new GDHoverInfo
        {
            Content = $"```gdscript\n(annotation) {text}\n```\n\n{description}",
            SymbolName = text,
            StartLine = attr.StartLine + 1,
            StartColumn = attr.StartColumn,
            EndLine = attr.EndLine + 1,
            EndColumn = attr.StartColumn + text.Length
        };
    }
}
