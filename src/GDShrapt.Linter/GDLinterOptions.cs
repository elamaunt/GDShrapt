using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Configuration options for the GDScript linter.
    /// </summary>
    public class GDLinterOptions
    {
        private readonly HashSet<string> _disabledRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _enabledRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GDLintSeverity> _severityOverrides = new Dictionary<string, GDLintSeverity>(StringComparer.OrdinalIgnoreCase);

        // Naming options
        /// <summary>
        /// Expected case for class names. Default: PascalCase.
        /// </summary>
        public NamingCase ClassNameCase { get; set; } = NamingCase.PascalCase;

        /// <summary>
        /// Expected case for function/method names. Default: snake_case.
        /// </summary>
        public NamingCase FunctionNameCase { get; set; } = NamingCase.SnakeCase;

        /// <summary>
        /// Expected case for variable names. Default: snake_case.
        /// </summary>
        public NamingCase VariableNameCase { get; set; } = NamingCase.SnakeCase;

        /// <summary>
        /// Expected case for constant names. Default: SCREAMING_SNAKE_CASE.
        /// </summary>
        public NamingCase ConstantNameCase { get; set; } = NamingCase.ScreamingSnakeCase;

        /// <summary>
        /// Expected case for signal names. Default: snake_case.
        /// </summary>
        public NamingCase SignalNameCase { get; set; } = NamingCase.SnakeCase;

        /// <summary>
        /// Expected case for enum names. Default: PascalCase.
        /// </summary>
        public NamingCase EnumNameCase { get; set; } = NamingCase.PascalCase;

        /// <summary>
        /// Expected case for enum values. Default: SCREAMING_SNAKE_CASE.
        /// </summary>
        public NamingCase EnumValueCase { get; set; } = NamingCase.ScreamingSnakeCase;

        /// <summary>
        /// Whether private members should be prefixed with underscore.
        /// </summary>
        public bool RequireUnderscoreForPrivate { get; set; } = true;

        // Style options
        /// <summary>
        /// Maximum allowed line length. 0 to disable. Default: 100.
        /// </summary>
        public int MaxLineLength { get; set; } = 100;

        /// <summary>
        /// Whether to require blank line after class declaration.
        /// </summary>
        public bool RequireBlankLineAfterClassDecl { get; set; } = true;

        /// <summary>
        /// Whether to require two blank lines between functions.
        /// </summary>
        public bool RequireTwoBlankLinesBetweenFunctions { get; set; } = true;

        /// <summary>
        /// Whether to require one blank line between class members of different types.
        /// </summary>
        public bool RequireBlankLineBetweenMemberTypes { get; set; } = true;

        // Best practices options
        /// <summary>
        /// Whether to warn about unused variables.
        /// </summary>
        public bool WarnUnusedVariables { get; set; } = true;

        /// <summary>
        /// Whether to warn about unused parameters.
        /// </summary>
        public bool WarnUnusedParameters { get; set; } = true;

        /// <summary>
        /// Whether to warn about unused signals.
        /// </summary>
        public bool WarnUnusedSignals { get; set; } = false;

        /// <summary>
        /// Whether to suggest using type hints.
        /// </summary>
        public bool SuggestTypeHints { get; set; } = false;

        /// <summary>
        /// Whether to warn about empty functions (functions with only pass).
        /// </summary>
        public bool WarnEmptyFunctions { get; set; } = true;

        /// <summary>
        /// Maximum number of parameters allowed in a function. 0 to disable.
        /// </summary>
        public int MaxParameters { get; set; } = 5;

        /// <summary>
        /// Maximum number of statements allowed in a function. 0 to disable.
        /// </summary>
        public int MaxFunctionLength { get; set; } = 50;

        // Organization options
        /// <summary>
        /// Whether to enforce member ordering (signals, enums, constants, vars, funcs).
        /// </summary>
        public bool EnforceMemberOrdering { get; set; } = false;

        /// <summary>
        /// Disables a rule by its ID.
        /// </summary>
        public void DisableRule(string ruleId)
        {
            _disabledRules.Add(ruleId);
            _enabledRules.Remove(ruleId);
        }

        /// <summary>
        /// Enables a rule by its ID.
        /// </summary>
        public void EnableRule(string ruleId)
        {
            _enabledRules.Add(ruleId);
            _disabledRules.Remove(ruleId);
        }

        /// <summary>
        /// Sets a custom severity for a rule.
        /// </summary>
        public void SetRuleSeverity(string ruleId, GDLintSeverity severity)
        {
            _severityOverrides[ruleId] = severity;
        }

        /// <summary>
        /// Checks if a rule is enabled.
        /// </summary>
        public bool IsRuleEnabled(GDLintRule rule)
        {
            if (_disabledRules.Contains(rule.RuleId))
                return false;

            if (_enabledRules.Contains(rule.RuleId))
                return true;

            return rule.EnabledByDefault;
        }

        /// <summary>
        /// Gets the effective severity for a rule.
        /// </summary>
        public GDLintSeverity GetRuleSeverity(GDLintRule rule)
        {
            if (_severityOverrides.TryGetValue(rule.RuleId, out var severity))
                return severity;

            return rule.DefaultSeverity;
        }

        /// <summary>
        /// Default options following GDScript style guide.
        /// </summary>
        public static GDLinterOptions Default => new GDLinterOptions();

        /// <summary>
        /// Strict options with all rules enabled.
        /// </summary>
        public static GDLinterOptions Strict => new GDLinterOptions
        {
            SuggestTypeHints = true,
            WarnUnusedSignals = true,
            EnforceMemberOrdering = true
        };

        /// <summary>
        /// Minimal options with only critical rules.
        /// </summary>
        public static GDLinterOptions Minimal => new GDLinterOptions
        {
            WarnUnusedVariables = false,
            WarnUnusedParameters = false,
            WarnEmptyFunctions = false,
            MaxLineLength = 0
        };
    }

    /// <summary>
    /// Naming case conventions.
    /// </summary>
    public enum NamingCase
    {
        /// <summary>
        /// snake_case
        /// </summary>
        SnakeCase,

        /// <summary>
        /// PascalCase
        /// </summary>
        PascalCase,

        /// <summary>
        /// camelCase
        /// </summary>
        CamelCase,

        /// <summary>
        /// SCREAMING_SNAKE_CASE
        /// </summary>
        ScreamingSnakeCase,

        /// <summary>
        /// Any case is allowed.
        /// </summary>
        Any
    }
}
