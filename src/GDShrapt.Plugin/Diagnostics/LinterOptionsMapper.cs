using GDShrapt.Plugin.Config;
using GDShrapt.Reader;

namespace GDShrapt.Plugin.Diagnostics;

/// <summary>
/// Maps plugin's AdvancedLintingConfig to GDShrapt.Linter's GDLinterOptions.
/// Provides seamless integration between the plugin configuration and the external linter library.
/// </summary>
internal static class LinterOptionsMapper
{
    /// <summary>
    /// Creates GDLinterOptions from plugin's AdvancedLintingConfig.
    /// </summary>
    /// <param name="config">Plugin linting configuration. If null, returns default options.</param>
    /// <returns>Configured GDLinterOptions instance.</returns>
    public static GDLinterOptions CreateOptions(AdvancedLintingConfig? config)
    {
        if (config == null)
            return GDLinterOptions.Default;

        var options = new GDLinterOptions
        {
            // Naming conventions
            ClassNameCase = MapNamingCase(config.ClassNameCase),
            FunctionNameCase = MapNamingCase(config.FunctionNameCase),
            VariableNameCase = MapNamingCase(config.VariableNameCase),
            ConstantNameCase = MapNamingCase(config.ConstantNameCase),
            SignalNameCase = MapNamingCase(config.SignalNameCase),
            EnumNameCase = MapNamingCase(config.EnumNameCase),
            EnumValueCase = MapNamingCase(config.EnumValueCase),
            RequireUnderscoreForPrivate = config.RequireUnderscoreForPrivate,

            // Best practices
            WarnUnusedVariables = config.WarnUnusedVariables,
            WarnUnusedParameters = config.WarnUnusedParameters,
            WarnUnusedSignals = config.WarnUnusedSignals,
            WarnEmptyFunctions = config.WarnEmptyFunctions,
            WarnMagicNumbers = config.WarnMagicNumbers,
            WarnVariableShadowing = config.WarnVariableShadowing,
            WarnAwaitInLoop = config.WarnAwaitInLoop,

            // Limits
            MaxParameters = config.MaxParameters,
            MaxFunctionLength = config.MaxFunctionLength,
            MaxCyclomaticComplexity = config.MaxCyclomaticComplexity,

            // Strict typing
            StrictTypingClassVariables = MapStrictTypingSeverity(config.StrictTypingClassVariables),
            StrictTypingLocalVariables = MapStrictTypingSeverity(config.StrictTypingLocalVariables),
            StrictTypingParameters = MapStrictTypingSeverity(config.StrictTypingParameters),
            StrictTypingReturnTypes = MapStrictTypingSeverity(config.StrictTypingReturnTypes),

            // Comment suppression
            EnableCommentSuppression = config.EnableCommentSuppression
        };

        return options;
    }

    /// <summary>
    /// Maps plugin NamingCase to GDShrapt.Linter NamingCase.
    /// </summary>
    private static Reader.NamingCase MapNamingCase(Config.NamingCase namingCase)
    {
        return namingCase switch
        {
            Config.NamingCase.SnakeCase => Reader.NamingCase.SnakeCase,
            Config.NamingCase.PascalCase => Reader.NamingCase.PascalCase,
            Config.NamingCase.CamelCase => Reader.NamingCase.CamelCase,
            Config.NamingCase.ScreamingSnakeCase => Reader.NamingCase.ScreamingSnakeCase,
            Config.NamingCase.Any => Reader.NamingCase.Any,
            _ => Reader.NamingCase.SnakeCase
        };
    }

    /// <summary>
    /// Maps plugin StrictTypingSeverity to GDLintSeverity.
    /// </summary>
    private static GDLintSeverity? MapStrictTypingSeverity(StrictTypingSeverity? severity)
    {
        if (severity == null)
            return null;

        return severity.Value switch
        {
            StrictTypingSeverity.Warning => GDLintSeverity.Warning,
            StrictTypingSeverity.Error => GDLintSeverity.Error,
            _ => GDLintSeverity.Warning
        };
    }

    /// <summary>
    /// Creates options from LintingConfig for basic rules.
    /// Used when AdvancedLintingConfig is not available.
    /// </summary>
    public static GDLinterOptions CreateFromBasicConfig(LintingConfig config)
    {
        var options = GDLinterOptions.Default;
        options.MaxLineLength = config.MaxLineLength;
        return options;
    }
}
