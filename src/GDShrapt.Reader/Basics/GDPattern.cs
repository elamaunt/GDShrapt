using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    public abstract class GDPattern : GDNode
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        public abstract string[] GeneratePatterns();

        static readonly Dictionary<Type, string[]> _patternsCache = new Dictionary<Type, string[]>();

        /// <summary>
        /// Ordered patterns by length descending
        /// </summary>
        string[] _sortedPatterns;

        (bool HasPatternsToMatch, string MatchedPattern) _lastPatternCheck;

        public bool IsCompleted { get; private set; }
        public string Sequence { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            _sequenceBuilder.Append(c);

            var check = CheckForPatterns();

            if (!check.HasLongerPatternsToMatch)
            {
                if (check.MatchedPattern == null)
                {
                    PatternMatched(_lastPatternCheck.MatchedPattern);
                    state.PopNode();
                    state.HandleChar(c);
                }
                else
                {
                    PatternMatched(check.MatchedPattern);
                    state.PopNode();
                }

                _sequenceBuilder.Clear();
                return;
            }

            _lastPatternCheck = check;
        }

        protected abstract void PatternMatched(string pattern);

        public (bool HasLongerPatternsToMatch, string MatchedPattern) CheckForPatterns()
        {
            var seq = _sequenceBuilder;

            if (seq == null || seq.Length == 0)
                return (true, null);

            InitPatterns();

            bool hasLongerPatternsToMatch = false;
            string matchedPattern = null;

            for (int i = 0; i < _sortedPatterns.Length; i++)
            {
                var word = _sortedPatterns[i];

                // Check pattern with same length
                if (word.Length == seq.Length)
                {
                    for (int k = 0; k < seq.Length; k++)
                    {
                        if (seq[k] != word[k])
                            goto CONTINUE;
                    }

                    matchedPattern = word;
                }
                else
                {
                    // Check pattern with longer length
                    if (seq.Length < word.Length)
                    {
                        for (int k = 0; k < seq.Length; k++)
                        {
                            if (seq[k] != word[k])
                                goto CONTINUE;
                        }

                        hasLongerPatternsToMatch = true;
                    }
                    else
                        break; // Dont check patterns with less length
                }

            CONTINUE: continue;
            }

            return (hasLongerPatternsToMatch, matchedPattern);
        }

        private void InitPatterns()
        {
            if (_sortedPatterns != null)
                return;

            var type = GetType();

            if (_patternsCache.TryGetValue(type, out _sortedPatterns))
                return;

            var patterns = GeneratePatterns();

            if (patterns == null || patterns.Length == 0)
                throw new Exception("Patterns list must contains at least one value.");

            // Prepare and save patterns
            _patternsCache[type] = _sortedPatterns = patterns.Distinct().OrderByDescending(x => x.Length).ToArray();
        }
    }
}