using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Detects duplicate keys in dictionary initializers.
    /// Duplicate keys will cause only the last value to be kept, which is likely a bug.
    /// </summary>
    public class GDDuplicateDictKeyRule : GDLintRule
    {
        public override string RuleId => "GDL214";
        public override string Name => "duplicate-dict-key";
        public override string Description => "Warn about duplicate dictionary keys";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Error;

        public override void Visit(GDDictionaryInitializerExpression dict)
        {
            if (dict?.KeyValues == null)
                return;

            var seenKeys = new Dictionary<string, GDSyntaxToken>();

            foreach (var kvp in dict.KeyValues)
            {
                if (kvp?.Key == null)
                    continue;

                var keyStr = kvp.Key.ToString();
                if (string.IsNullOrWhiteSpace(keyStr))
                    continue;

                // Get the first token of the key for position reporting
                var keyToken = kvp.Key.AllTokens.FirstOrDefault() ?? (GDSyntaxToken)kvp.Colon ?? kvp.Assign;

                if (seenKeys.TryGetValue(keyStr, out var firstOccurrence))
                {
                    ReportIssue(
                        $"Duplicate dictionary key: '{keyStr}'",
                        keyToken,
                        "Only the last value will be kept. Remove the duplicate or use a different key.");
                }
                else
                {
                    seenKeys[keyStr] = keyToken;
                }
            }
        }
    }
}
