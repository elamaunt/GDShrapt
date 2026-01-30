using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    internal abstract class GDPatternResolver : GDResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        public abstract string[] GeneratePatterns();

        static readonly ConcurrentDictionary<Type, string[]> _patternsCache = new ConcurrentDictionary<Type, string[]>();

        /// <summary>
        /// Ordered patterns by length descending
        /// </summary>
        string[] _sortedPatterns;

        (bool HasPatternsToMatch, string MatchedPattern) _lastPatternCheck;

        public bool IsCompleted { get; private set; }
        public string Sequence { get; set; }

        public GDPatternResolver(ITokenReceiver owner)
            : base(owner)
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            _sequenceBuilder.Append(c);

            var check = CheckForPatterns();

            if (!check.HasLongerPatternsToMatch)
            {
                state.Pop();

                if (check.MatchedPattern == null)
                    CompleteWithPattern(_lastPatternCheck.MatchedPattern, state);
                else
                    CompleteWithPattern(check.MatchedPattern, state);

                _sequenceBuilder.Clear();
                return;
            }

            _lastPatternCheck = (check.HasLongerPatternsToMatch, check.MatchedPattern ?? _lastPatternCheck.MatchedPattern);
        }

        private void CompleteWithPattern(string matchedPattern, GDReadingState state)
        {
            if (IsCompleted)
            {
                state.Pop();
                return;
            }

            IsCompleted = true;

            PatternMatched(matchedPattern, state);

            for (int i = matchedPattern?.Length ?? 0; i < _sequenceBuilder.Length; i++)
                state.PassChar(_sequenceBuilder[i]);
        }

        protected abstract void PatternMatched(string pattern, GDReadingState state);

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

            _sortedPatterns = _patternsCache.GetOrAdd(type, t =>
            {
                var patterns = GeneratePatterns();

                if (patterns == null || patterns.Length == 0)
                    throw new Exception("Patterns list must contains at least one value.");

                return patterns.Distinct().OrderByDescending(x => x.Length).ToArray();
            });
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            HandleChar('\n', state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            HandleChar('\\', state);
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            // CR should complete pattern matching just like newline does
            // Pop first, then complete with any matched pattern, then pass CR
            state.Pop();
            CompleteWithPattern(_lastPatternCheck.MatchedPattern, state);
            _sequenceBuilder.Clear();
            state.PassCarriageReturnChar();
        }

        internal override void ForceComplete(GDReadingState state)
        {
            CompleteWithPattern(_lastPatternCheck.MatchedPattern, state);
        }
    }
}