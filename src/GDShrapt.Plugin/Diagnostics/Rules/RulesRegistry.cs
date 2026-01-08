using GDShrapt.Plugin.Config;
using GDShrapt.Plugin.Diagnostics.Rules.Formatting;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin.Diagnostics.Rules;

/// <summary>
/// Registry of all available lint rules.
/// Includes both plugin-native rules and rules from GDShrapt.Linter.
/// </summary>
internal static class RulesRegistry
{
    private static readonly List<ILintRule> _rules;
    private static bool _gdLinterRulesLoaded = false;

    static RulesRegistry()
    {
        _rules = new List<ILintRule>
        {
            // Light Formatting Rules (GDS001-009)
            new IndentationRule(),           // GDS001
            new TrailingWhitespaceRule(),    // GDS002
            new TrailingNewlineRule(),       // GDS003

            // Full Formatting Rules (GDS010-019)
            new SpaceAroundOperatorsRule(),  // GDS010
            new SpaceAfterCommaRule(),       // GDS011
            new EmptyLineRule(),             // GDS013
        };

        // Load GDShrapt.Linter rules
        LoadGDLinterRules();
    }

    /// <summary>
    /// Loads rules from GDShrapt.Linter library.
    /// </summary>
    private static void LoadGDLinterRules()
    {
        if (_gdLinterRulesLoaded)
            return;

        try
        {
            // Create a default linter to get all its registered rules
            var linter = new GDLinter();

            foreach (var linterRule in linter.Rules)
            {
                // Wrap each GDLinter rule in our adapter
                var adapter = new GDLinterRuleAdapter(linterRule);
                _rules.Add(adapter);
            }

            _gdLinterRulesLoaded = true;
            Logger.Info($"Loaded {linter.Rules.Count} rules from GDShrapt.Linter");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load GDShrapt.Linter rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all registered rules.
    /// </summary>
    public static IReadOnlyList<ILintRule> GetAllRules() => _rules;

    /// <summary>
    /// Gets a rule by its ID.
    /// </summary>
    public static ILintRule? GetRule(string ruleId)
    {
        return _rules.FirstOrDefault(r => r.RuleId == ruleId);
    }

    /// <summary>
    /// Gets rules by category.
    /// </summary>
    public static IEnumerable<ILintRule> GetRulesByCategory(DiagnosticCategory category)
    {
        return _rules.Where(r => r.Category == category);
    }

    /// <summary>
    /// Gets rules that apply at a specific formatting level.
    /// </summary>
    public static IEnumerable<ILintRule> GetRulesForFormattingLevel(FormattingLevel level)
    {
        return _rules.Where(r =>
            r.Category == DiagnosticCategory.Formatting &&
            r.RequiredFormattingLevel <= level);
    }

    /// <summary>
    /// Gets all formatting rules.
    /// </summary>
    public static IEnumerable<ILintRule> GetFormattingRules()
    {
        return _rules.Where(r => r.Category == DiagnosticCategory.Formatting);
    }

    /// <summary>
    /// Gets all style rules.
    /// </summary>
    public static IEnumerable<ILintRule> GetStyleRules()
    {
        return _rules.Where(r => r.Category == DiagnosticCategory.Style);
    }

    /// <summary>
    /// Gets count of registered rules.
    /// </summary>
    public static int Count => _rules.Count;

    /// <summary>
    /// Gets rule descriptions for help/documentation.
    /// </summary>
    public static IEnumerable<RuleInfo> GetRuleInfos()
    {
        return _rules.Select(r => new RuleInfo
        {
            RuleId = r.RuleId,
            Name = r.Name,
            Description = r.Description,
            Category = r.Category,
            DefaultSeverity = r.DefaultSeverity,
            RequiredFormattingLevel = r.RequiredFormattingLevel
        });
    }
}

/// <summary>
/// Information about a rule for documentation/UI.
/// </summary>
internal class RuleInfo
{
    public required string RuleId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DiagnosticCategory Category { get; init; }
    public DiagnosticSeverity DefaultSeverity { get; init; }
    public FormattingLevel RequiredFormattingLevel { get; init; }
}
