using System;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDIntendation : GDSimpleSyntaxToken
    {
        int _lineIntendationInSpaces;

        internal StringBuilder _sequenceBuilder = new StringBuilder();

        public int LineIntendationThreshold { get; set; }
        public string Sequence { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == '\t')
            {
                _sequenceBuilder.Append(c);
                _lineIntendationInSpaces += state.Settings.SingleTabSpacesCost;
                return;
            }

            if (c == ' ')
            {
                _sequenceBuilder.Append(c);
                _lineIntendationInSpaces++;
                return;
            }

            Sequence = _sequenceBuilder.ToString();
            LineIntendationThreshold = _lineIntendationInSpaces;

            state.PopAndPass(c);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDIntendation()
            {
                Sequence = Sequence
            };
        }
        /// <summary>
        /// Counts all <see cref="GDIntendedNode"/> in parents and updates <see cref="LineIntendationThreshold"/> with the count.
        /// Also updates the <see cref="Sequence"/>.
        /// </summary>
        /// <param name="pattern">Whitespace pattern. Used as a single intendation. If null, a single \t character is used.</param>
        /// <exception cref="ArgumentException">If used not a whitespace pattern</exception>
        public void Update(string pattern = null)
        {
            if (pattern == null)
                pattern = "\t";

            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Intendation pattern must not be empty.");
            if (!string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Intendation pattern must be a whitespace.");

            var p = Parent;
            int intendation = 0;

            while (p != null)
            {
                if (p is GDIntendedNode)
                    intendation++;

                p = p.Parent;
            }

            LineIntendationThreshold = intendation;

            if (intendation == 0)
                Sequence = string.Empty;
            else if (intendation == 1)
                Sequence = pattern;
            else
            {
                var builder = new StringBuilder(pattern.Length * intendation);

                for (int i = 0; i < intendation; i++)
                    builder.Append(pattern);

                Sequence = builder.ToString();
            }
        }

        /// <summary>
        /// Converts the indentation pattern (tabs to spaces or vice versa) without recalculating the level.
        /// This preserves the existing indentation structure while changing the whitespace style.
        /// </summary>
        /// <param name="pattern">The new indentation pattern for a single level. If null, uses tab.</param>
        /// <param name="tabSize">Number of spaces that equal one tab. Default is 4.</param>
        public void ConvertPattern(string pattern = null, int tabSize = 4)
        {
            if (pattern == null)
                pattern = "\t";

            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Indentation pattern must not be empty.");
            if (!string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Indentation pattern must be whitespace.");

            if (string.IsNullOrEmpty(Sequence))
                return;

            // Calculate current indentation level from the existing sequence
            int currentLevel = CalculateLevel(Sequence, tabSize);

            if (currentLevel == 0)
            {
                Sequence = string.Empty;
            }
            else if (currentLevel == 1)
            {
                Sequence = pattern;
            }
            else
            {
                var builder = new StringBuilder(pattern.Length * currentLevel);
                for (int i = 0; i < currentLevel; i++)
                    builder.Append(pattern);
                Sequence = builder.ToString();
            }

            LineIntendationThreshold = currentLevel;
        }

        /// <summary>
        /// Calculates the indentation level from a sequence string.
        /// </summary>
        private static int CalculateLevel(string sequence, int tabSize)
        {
            if (string.IsNullOrEmpty(sequence))
                return 0;

            int spaces = 0;
            foreach (char c in sequence)
            {
                if (c == '\t')
                    spaces += tabSize;
                else if (c == ' ')
                    spaces++;
            }

            return spaces / tabSize;
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}
