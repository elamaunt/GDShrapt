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

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}
