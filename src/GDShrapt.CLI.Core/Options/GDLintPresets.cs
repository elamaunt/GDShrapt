using GDShrapt.Linter;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Built-in lint presets that provide curated sets of linter option overrides.
/// Individual CLI flags still override preset values.
/// </summary>
public static class GDLintPresets
{
    public static readonly string[] AvailablePresets = { "strict", "relaxed", "recommended", "gdquest" };

    public static GDLinterOptionsOverrides? GetPreset(string? name) => name?.ToLowerInvariant() switch
    {
        "strict" => Strict(),
        "relaxed" => Relaxed(),
        "recommended" => Recommended(),
        "gdquest" => GDQuest(),
        _ => null
    };

    private static GDLinterOptionsOverrides Strict() => new()
    {
        // Naming conventions (standard GDScript)
        ClassNameCase = NamingCase.PascalCase,
        FunctionNameCase = NamingCase.SnakeCase,
        VariableNameCase = NamingCase.SnakeCase,
        ConstantNameCase = NamingCase.ScreamingSnakeCase,
        SignalNameCase = NamingCase.SnakeCase,
        EnumNameCase = NamingCase.PascalCase,
        EnumValueCase = NamingCase.ScreamingSnakeCase,
        InnerClassNameCase = NamingCase.PascalCase,
        RequireUnderscoreForPrivate = true,

        // Tight limits
        MaxLineLength = 100,
        MaxFileLines = 300,
        MaxParameters = 4,
        MaxFunctionLength = 30,
        MaxCyclomaticComplexity = 6,
        MaxPublicMethods = 10,
        MaxReturns = 3,
        MaxNestingDepth = 3,
        MaxLocalVariables = 5,
        MaxClassVariables = 10,
        MaxBranches = 8,
        MaxBooleanExpressions = 3,
        MaxInnerClasses = 2,

        // All warnings enabled
        WarnUnusedVariables = true,
        WarnUnusedParameters = true,
        WarnUnusedSignals = true,
        WarnEmptyFunctions = true,
        WarnMagicNumbers = true,
        WarnVariableShadowing = true,
        WarnAwaitInLoop = true,
        WarnNoElifReturn = true,
        WarnNoElseReturn = true,
        WarnPrivateMethodCall = true,
        WarnDuplicatedLoad = true,
        WarnExpressionNotAssigned = true,
        WarnUselessAssignment = true,
        WarnInconsistentReturn = true,
        WarnMissingReturn = true,
        WarnNoLonelyIf = true,
        WarnGodClass = true,
        WarnCommentedCode = true,
        WarnDebugPrint = true,

        // God class thresholds (tight)
        GodClassMaxVariables = 10,
        GodClassMaxMethods = 15,
        GodClassMaxLines = 300,

        // Strict typing everywhere
        StrictTypingClassVariables = GDLintSeverity.Error,
        StrictTypingLocalVariables = GDLintSeverity.Error,
        StrictTypingParameters = GDLintSeverity.Error,
        StrictTypingReturnTypes = GDLintSeverity.Error,

        // Suppression enabled
        EnableCommentSuppression = true,

        // Formatting/style
        IndentationStyle = Linter.GDIndentationStyle.Tabs,
        TabWidth = 4,
        CheckTrailingWhitespace = true,
        CheckTrailingNewline = true,
        CheckSpaceAroundOperators = true,
        CheckSpaceAfterComma = true,

        // Blank lines
        EmptyLinesBetweenFunctions = 2,
        MaxConsecutiveEmptyLines = 2,
        RequireBlankLineAfterClassDecl = true,
        RequireTwoBlankLinesBetweenFunctions = true,
        RequireBlankLineBetweenMemberTypes = true,

        // Best practices
        SuggestTypeHints = true,
        RequireTrailingComma = true,
        EnforceMemberOrdering = true,

        // Magic numbers whitelist
        AllowedMagicNumbers = "0,1,-1"
    };

    private static GDLinterOptionsOverrides Relaxed() => new()
    {
        // Naming conventions (standard GDScript, but flexible)
        ClassNameCase = NamingCase.PascalCase,
        FunctionNameCase = NamingCase.SnakeCase,
        VariableNameCase = NamingCase.SnakeCase,
        ConstantNameCase = NamingCase.ScreamingSnakeCase,
        SignalNameCase = NamingCase.SnakeCase,
        EnumNameCase = NamingCase.PascalCase,
        EnumValueCase = NamingCase.ScreamingSnakeCase,
        InnerClassNameCase = NamingCase.PascalCase,
        RequireUnderscoreForPrivate = false,

        // Generous limits
        MaxLineLength = 120,
        MaxFileLines = 1000,
        MaxParameters = 8,
        MaxFunctionLength = 80,
        MaxCyclomaticComplexity = 15,
        MaxPublicMethods = 25,
        MaxReturns = 8,
        MaxNestingDepth = 6,
        MaxLocalVariables = 15,
        MaxClassVariables = 25,
        MaxBranches = 15,
        MaxBooleanExpressions = 5,
        MaxInnerClasses = 5,

        // Only critical warnings
        WarnUnusedVariables = true,
        WarnUnusedParameters = false,
        WarnUnusedSignals = false,
        WarnEmptyFunctions = false,
        WarnMagicNumbers = false,
        WarnVariableShadowing = true,
        WarnAwaitInLoop = true,
        WarnNoElifReturn = false,
        WarnNoElseReturn = false,
        WarnPrivateMethodCall = false,
        WarnDuplicatedLoad = true,
        WarnExpressionNotAssigned = false,
        WarnUselessAssignment = true,
        WarnInconsistentReturn = true,
        WarnMissingReturn = true,
        WarnNoLonelyIf = false,
        WarnGodClass = false,
        WarnCommentedCode = false,
        WarnDebugPrint = false,

        // God class thresholds (generous)
        GodClassMaxVariables = 25,
        GodClassMaxMethods = 30,
        GodClassMaxLines = 1000,

        // No strict typing
        StrictTypingClassVariables = GDLintSeverity.Hint,
        StrictTypingLocalVariables = GDLintSeverity.Hint,
        StrictTypingParameters = GDLintSeverity.Hint,
        StrictTypingReturnTypes = GDLintSeverity.Hint,

        // Suppression enabled
        EnableCommentSuppression = true,

        // Formatting/style (minimal checks)
        IndentationStyle = Linter.GDIndentationStyle.Tabs,
        TabWidth = 4,
        CheckTrailingWhitespace = false,
        CheckTrailingNewline = false,
        CheckSpaceAroundOperators = false,
        CheckSpaceAfterComma = false,

        // Blank lines (minimal)
        MaxConsecutiveEmptyLines = 3,
        RequireBlankLineAfterClassDecl = false,
        RequireTwoBlankLinesBetweenFunctions = false,
        RequireBlankLineBetweenMemberTypes = false,

        // Best practices (minimal)
        SuggestTypeHints = false,
        RequireTrailingComma = false,
        EnforceMemberOrdering = false,

        // Magic numbers whitelist (generous)
        AllowedMagicNumbers = "0,1,2,-1,0.5,100"
    };

    private static GDLinterOptionsOverrides Recommended() => new()
    {
        // Naming conventions (standard GDScript)
        ClassNameCase = NamingCase.PascalCase,
        FunctionNameCase = NamingCase.SnakeCase,
        VariableNameCase = NamingCase.SnakeCase,
        ConstantNameCase = NamingCase.ScreamingSnakeCase,
        SignalNameCase = NamingCase.SnakeCase,
        EnumNameCase = NamingCase.PascalCase,
        EnumValueCase = NamingCase.ScreamingSnakeCase,
        InnerClassNameCase = NamingCase.PascalCase,
        RequireUnderscoreForPrivate = true,

        // Balanced limits
        MaxLineLength = 100,
        MaxFileLines = 500,
        MaxParameters = 5,
        MaxFunctionLength = 50,
        MaxCyclomaticComplexity = 10,
        MaxPublicMethods = 15,
        MaxReturns = 5,
        MaxNestingDepth = 4,
        MaxLocalVariables = 8,
        MaxClassVariables = 15,
        MaxBranches = 12,
        MaxBooleanExpressions = 4,
        MaxInnerClasses = 3,

        // Most warnings enabled
        WarnUnusedVariables = true,
        WarnUnusedParameters = true,
        WarnUnusedSignals = true,
        WarnEmptyFunctions = true,
        WarnMagicNumbers = true,
        WarnVariableShadowing = true,
        WarnAwaitInLoop = true,
        WarnNoElifReturn = true,
        WarnNoElseReturn = true,
        WarnPrivateMethodCall = true,
        WarnDuplicatedLoad = true,
        WarnExpressionNotAssigned = true,
        WarnUselessAssignment = true,
        WarnInconsistentReturn = true,
        WarnMissingReturn = true,
        WarnNoLonelyIf = true,
        WarnGodClass = true,
        WarnCommentedCode = false,
        WarnDebugPrint = true,

        // God class thresholds (balanced)
        GodClassMaxVariables = 15,
        GodClassMaxMethods = 20,
        GodClassMaxLines = 500,

        // Strict typing as warnings
        StrictTypingClassVariables = GDLintSeverity.Warning,
        StrictTypingLocalVariables = GDLintSeverity.Hint,
        StrictTypingParameters = GDLintSeverity.Warning,
        StrictTypingReturnTypes = GDLintSeverity.Warning,

        // Suppression enabled
        EnableCommentSuppression = true,

        // Formatting/style
        IndentationStyle = Linter.GDIndentationStyle.Tabs,
        TabWidth = 4,
        CheckTrailingWhitespace = true,
        CheckTrailingNewline = true,
        CheckSpaceAroundOperators = true,
        CheckSpaceAfterComma = true,

        // Blank lines
        EmptyLinesBetweenFunctions = 2,
        MaxConsecutiveEmptyLines = 2,
        RequireBlankLineAfterClassDecl = true,
        RequireTwoBlankLinesBetweenFunctions = true,
        RequireBlankLineBetweenMemberTypes = true,

        // Best practices
        SuggestTypeHints = true,
        RequireTrailingComma = false,
        EnforceMemberOrdering = true,

        // Magic numbers whitelist
        AllowedMagicNumbers = "0,1,2,-1"
    };

    private static GDLinterOptionsOverrides GDQuest() => new()
    {
        // GDQuest naming conventions
        ClassNameCase = NamingCase.PascalCase,
        FunctionNameCase = NamingCase.SnakeCase,
        VariableNameCase = NamingCase.SnakeCase,
        ConstantNameCase = NamingCase.ScreamingSnakeCase,
        SignalNameCase = NamingCase.SnakeCase,
        EnumNameCase = NamingCase.PascalCase,
        EnumValueCase = NamingCase.ScreamingSnakeCase,
        InnerClassNameCase = NamingCase.PascalCase,
        RequireUnderscoreForPrivate = true,

        // GDQuest-style limits
        MaxLineLength = 100,
        MaxFileLines = 450,
        MaxParameters = 5,
        MaxFunctionLength = 40,
        MaxCyclomaticComplexity = 10,
        MaxPublicMethods = 15,
        MaxReturns = 5,
        MaxNestingDepth = 4,
        MaxLocalVariables = 8,
        MaxClassVariables = 12,
        MaxBranches = 10,
        MaxBooleanExpressions = 3,
        MaxInnerClasses = 2,

        // GDQuest-style warnings
        WarnUnusedVariables = true,
        WarnUnusedParameters = true,
        WarnUnusedSignals = true,
        WarnEmptyFunctions = true,
        WarnMagicNumbers = true,
        WarnVariableShadowing = true,
        WarnAwaitInLoop = true,
        WarnNoElifReturn = true,
        WarnNoElseReturn = true,
        WarnPrivateMethodCall = true,
        WarnDuplicatedLoad = true,
        WarnExpressionNotAssigned = true,
        WarnUselessAssignment = true,
        WarnInconsistentReturn = true,
        WarnMissingReturn = true,
        WarnNoLonelyIf = true,
        WarnGodClass = true,
        WarnCommentedCode = true,
        WarnDebugPrint = true,

        // God class thresholds (GDQuest-style)
        GodClassMaxVariables = 12,
        GodClassMaxMethods = 15,
        GodClassMaxLines = 450,

        // Type hints strongly encouraged
        StrictTypingClassVariables = GDLintSeverity.Warning,
        StrictTypingLocalVariables = GDLintSeverity.Warning,
        StrictTypingParameters = GDLintSeverity.Warning,
        StrictTypingReturnTypes = GDLintSeverity.Warning,

        // Suppression enabled
        EnableCommentSuppression = true,

        // Formatting/style (tabs, GDQuest standard)
        IndentationStyle = Linter.GDIndentationStyle.Tabs,
        TabWidth = 4,
        CheckTrailingWhitespace = true,
        CheckTrailingNewline = true,
        CheckSpaceAroundOperators = true,
        CheckSpaceAfterComma = true,

        // Blank lines (GDQuest style: 2 lines between functions)
        EmptyLinesBetweenFunctions = 2,
        MaxConsecutiveEmptyLines = 2,
        RequireBlankLineAfterClassDecl = true,
        RequireTwoBlankLinesBetweenFunctions = true,
        RequireBlankLineBetweenMemberTypes = true,

        // Best practices
        SuggestTypeHints = true,
        RequireTrailingComma = true,
        EnforceMemberOrdering = true,

        // Magic numbers whitelist
        AllowedMagicNumbers = "0,1,2,-1,0.5"
    };
}
