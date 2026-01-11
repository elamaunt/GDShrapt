using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// GDScript linter that checks code against configurable rules.
    /// </summary>
    public class GDLinter
    {
        private readonly List<GDLintRule> _rules = new List<GDLintRule>();
        private readonly GDScriptReader _reader = new GDScriptReader();

        /// <summary>
        /// Linter options.
        /// </summary>
        public GDLinterOptions Options { get; set; } = GDLinterOptions.Default;

        /// <summary>
        /// All registered rules.
        /// </summary>
        public IReadOnlyList<GDLintRule> Rules => _rules;

        /// <summary>
        /// Creates a new linter with default GDScript style guide rules.
        /// </summary>
        public GDLinter()
        {
            RegisterDefaultRules();
        }

        /// <summary>
        /// Creates a new linter with specified options and default rules.
        /// </summary>
        public GDLinter(GDLinterOptions options) : this()
        {
            Options = options ?? GDLinterOptions.Default;
        }

        /// <summary>
        /// Creates a new linter without any rules.
        /// Use AddRule to add custom rules.
        /// </summary>
        public static GDLinter CreateEmpty()
        {
            var linter = new GDLinter();
            linter._rules.Clear();
            return linter;
        }

        /// <summary>
        /// Registers the default GDScript style guide rules.
        /// </summary>
        private void RegisterDefaultRules()
        {
            // Naming rules
            AddRule(new GDClassNameCaseRule());
            AddRule(new GDFunctionNameCaseRule());
            AddRule(new GDVariableNameCaseRule());
            AddRule(new GDConstantNameCaseRule());
            AddRule(new GDSignalNameCaseRule());
            AddRule(new GDEnumNameCaseRule());
            AddRule(new GDEnumValueCaseRule());
            AddRule(new GDPrivatePrefixRule());
            AddRule(new GDInnerClassNameCaseRule());

            // Style rules
            AddRule(new GDLineLengthRule());
            AddRule(new GDMaxFileLinesRule());
            AddRule(new GDNoElifReturnRule());
            AddRule(new GDNoElseReturnRule());

            // Best practices rules
            AddRule(new GDUnusedVariableRule());
            AddRule(new GDUnusedParameterRule());
            AddRule(new GDEmptyFunctionRule());
            AddRule(new GDTypeHintRule());
            AddRule(new GDMaxParametersRule());
            AddRule(new GDMaxFunctionLengthRule());
            AddRule(new GDUnusedSignalRule());
            AddRule(new GDCyclomaticComplexityRule());
            AddRule(new GDMagicNumberRule());
            AddRule(new GDDeadCodeRule());
            AddRule(new GDVariableShadowingRule());
            AddRule(new GDAwaitInLoopRule());
            AddRule(new GDSelfComparisonRule());
            AddRule(new GDDuplicateDictKeyRule());
            AddRule(new GDStrictTypingRule());
            AddRule(new GDPrivateMethodCallRule());
            AddRule(new GDDuplicatedLoadRule());
            AddRule(new GDAbstractMethodBodyRule());
            AddRule(new GDAbstractClassRequiredRule());
            AddRule(new GDExpressionNotAssignedRule());
            AddRule(new GDNoSelfAssignRule());
            AddRule(new GDUselessAssignmentRule());
            AddRule(new GDConsistentReturnRule());

            // Style rules
            AddRule(new GDTrailingCommaRule());
            AddRule(new GDNoLonelyIfRule());

            // Complexity rules
            AddRule(new GDMaxPublicMethodsRule());
            AddRule(new GDMaxReturnsRule());
            AddRule(new GDMaxNestingDepthRule());
            AddRule(new GDMaxLocalVariablesRule());
            AddRule(new GDMaxClassVariablesRule());
            AddRule(new GDMaxBranchesRule());
            AddRule(new GDMaxBooleanExpressionsRule());
            AddRule(new GDMaxInnerClassesRule());

            // Organization rules
            AddRule(new GDMemberOrderingRule());
        }

        /// <summary>
        /// Adds a custom rule to the linter.
        /// </summary>
        public void AddRule(GDLintRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            _rules.Add(rule);
        }

        /// <summary>
        /// Removes a rule by its ID.
        /// </summary>
        public bool RemoveRule(string ruleId)
        {
            return _rules.RemoveAll(r => r.RuleId.Equals(ruleId, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Gets a rule by its ID.
        /// </summary>
        public GDLintRule GetRule(string ruleId)
        {
            return _rules.FirstOrDefault(r => r.RuleId.Equals(ruleId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lints a parsed GDScript node.
        /// </summary>
        public GDLintResult Lint(GDNode node)
        {
            var result = new GDLintResult();

            if (node == null)
                return result;

            // Parse suppression directives from comments
            GDSuppressionContext suppressionContext = null;
            if (Options.EnableCommentSuppression)
            {
                suppressionContext = GDSuppressionParser.Parse(node);
            }

            foreach (var rule in _rules)
            {
                if (Options.IsRuleEnabled(rule))
                {
                    rule.Run(node, result, Options);
                }
            }

            // Filter out suppressed issues
            if (suppressionContext != null)
            {
                result.FilterSuppressed(suppressionContext);
            }

            return result;
        }

        /// <summary>
        /// Parses and lints GDScript source code.
        /// </summary>
        public GDLintResult LintCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return new GDLintResult();

            var tree = _reader.ParseFileContent(code);
            return Lint(tree);
        }

        /// <summary>
        /// Lints a GDScript expression.
        /// </summary>
        public GDLintResult LintExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return new GDLintResult();

            var expr = _reader.ParseExpression(expression);
            return Lint(expr);
        }

        /// <summary>
        /// Gets all rules in a specific category.
        /// </summary>
        public IEnumerable<GDLintRule> GetRulesByCategory(GDLintCategory category)
        {
            return _rules.Where(r => r.Category == category);
        }

        /// <summary>
        /// Gets all enabled rules.
        /// </summary>
        public IEnumerable<GDLintRule> GetEnabledRules()
        {
            return _rules.Where(r => Options.IsRuleEnabled(r));
        }

        /// <summary>
        /// Gets all disabled rules.
        /// </summary>
        public IEnumerable<GDLintRule> GetDisabledRules()
        {
            return _rules.Where(r => !Options.IsRuleEnabled(r));
        }
    }
}
