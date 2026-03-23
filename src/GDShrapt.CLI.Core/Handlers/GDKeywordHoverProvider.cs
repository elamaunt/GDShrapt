using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

internal static class GDKeywordHoverProvider
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
            _ => ((string?)null, (string?)null)
        };

        if (text == null || description == null)
            return null;

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
}
