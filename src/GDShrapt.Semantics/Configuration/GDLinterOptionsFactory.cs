using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Factory for creating GDLinterOptions from configuration.
/// Centralizes the mapping logic used by CLI, LSP, and Plugin.
/// </summary>
public static class GDLinterOptionsFactory
{
    /// <summary>
    /// Creates GDLinterOptions from project configuration.
    /// </summary>
    /// <param name="config">Project configuration.</param>
    /// <returns>Configured GDLinterOptions instance.</returns>
    public static GDLinterOptions FromConfig(GDProjectConfig config)
    {
        return FromConfig(config.AdvancedLinting, config.Linting.MaxLineLength);
    }

    /// <summary>
    /// Creates GDLinterOptions from advanced linting configuration.
    /// </summary>
    /// <param name="advanced">Advanced linting configuration.</param>
    /// <param name="maxLineLength">Maximum line length override.</param>
    /// <returns>Configured GDLinterOptions instance.</returns>
    public static GDLinterOptions FromConfig(GDAdvancedLintingConfig advanced, int maxLineLength = 120)
    {
        return new GDLinterOptions
        {
            // Naming conventions
            ClassNameCase = MapNamingCase(advanced.ClassNameCase),
            FunctionNameCase = MapNamingCase(advanced.FunctionNameCase),
            VariableNameCase = MapNamingCase(advanced.VariableNameCase),
            ConstantNameCase = MapNamingCase(advanced.ConstantNameCase),
            SignalNameCase = MapNamingCase(advanced.SignalNameCase),
            EnumNameCase = MapNamingCase(advanced.EnumNameCase),
            EnumValueCase = MapNamingCase(advanced.EnumValueCase),
            InnerClassNameCase = MapNamingCase(advanced.InnerClassNameCase),
            RequireUnderscoreForPrivate = advanced.RequireUnderscoreForPrivate,

            // Best practices
            WarnUnusedVariables = advanced.WarnUnusedVariables,
            WarnUnusedParameters = advanced.WarnUnusedParameters,
            WarnUnusedSignals = advanced.WarnUnusedSignals,
            WarnEmptyFunctions = advanced.WarnEmptyFunctions,
            WarnMagicNumbers = advanced.WarnMagicNumbers,
            WarnVariableShadowing = advanced.WarnVariableShadowing,
            WarnAwaitInLoop = advanced.WarnAwaitInLoop,
            WarnNoElifReturn = advanced.WarnNoElifReturn,
            WarnNoElseReturn = advanced.WarnNoElseReturn,
            WarnPrivateMethodCall = advanced.WarnPrivateMethodCall,
            WarnDuplicatedLoad = advanced.WarnDuplicatedLoad,
            WarnExpressionNotAssigned = advanced.WarnExpressionNotAssigned,
            WarnUselessAssignment = advanced.WarnUselessAssignment,
            WarnInconsistentReturn = advanced.WarnInconsistentReturn,
            WarnNoLonelyIf = advanced.WarnNoLonelyIf,

            // Limits
            MaxParameters = advanced.MaxParameters,
            MaxFunctionLength = advanced.MaxFunctionLength,
            MaxCyclomaticComplexity = advanced.MaxCyclomaticComplexity,
            MaxLineLength = maxLineLength,
            MaxFileLines = advanced.MaxFileLines,

            // Complexity limits
            MaxPublicMethods = advanced.MaxPublicMethods,
            MaxReturns = advanced.MaxReturns,
            MaxNestingDepth = advanced.MaxNestingDepth,
            MaxLocalVariables = advanced.MaxLocalVariables,
            MaxClassVariables = advanced.MaxClassVariables,
            MaxBranches = advanced.MaxBranches,
            MaxBooleanExpressions = advanced.MaxBooleanExpressions,
            MaxInnerClasses = advanced.MaxInnerClasses,

            // Strict typing
            StrictTypingClassVariables = MapStrictTypingSeverity(advanced.StrictTypingClassVariables),
            StrictTypingLocalVariables = MapStrictTypingSeverity(advanced.StrictTypingLocalVariables),
            StrictTypingParameters = MapStrictTypingSeverity(advanced.StrictTypingParameters),
            StrictTypingReturnTypes = MapStrictTypingSeverity(advanced.StrictTypingReturnTypes),

            // Comment suppression
            EnableCommentSuppression = advanced.EnableCommentSuppression,

            // Member ordering
            AbstractMethodPosition = advanced.AbstractMethodPosition,
            PrivateMethodPosition = advanced.PrivateMethodPosition,
            StaticMethodPosition = advanced.StaticMethodPosition
        };
    }

    /// <summary>
    /// Creates default GDLinterOptions when no configuration is available.
    /// </summary>
    public static GDLinterOptions CreateDefault()
    {
        return GDLinterOptions.Default;
    }

    /// <summary>
    /// Maps GDNamingCase from configuration to NamingCase used by linter.
    /// </summary>
    public static NamingCase MapNamingCase(GDNamingCase namingCase)
    {
        return namingCase switch
        {
            GDNamingCase.SnakeCase => NamingCase.SnakeCase,
            GDNamingCase.PascalCase => NamingCase.PascalCase,
            GDNamingCase.CamelCase => NamingCase.CamelCase,
            GDNamingCase.ScreamingSnakeCase => NamingCase.ScreamingSnakeCase,
            GDNamingCase.Any => NamingCase.Any,
            _ => NamingCase.SnakeCase
        };
    }

    /// <summary>
    /// Maps GDStrictTypingSeverity from configuration to GDLintSeverity used by linter.
    /// </summary>
    public static GDLintSeverity? MapStrictTypingSeverity(GDStrictTypingSeverity? severity)
    {
        if (!severity.HasValue)
            return null;

        return severity.Value switch
        {
            GDStrictTypingSeverity.Warning => GDLintSeverity.Warning,
            GDStrictTypingSeverity.Error => GDLintSeverity.Error,
            _ => GDLintSeverity.Warning
        };
    }
}
