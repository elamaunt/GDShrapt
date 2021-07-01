using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDIntendation : GDSimpleSyntaxToken
    {
        int _lineIntendation;
        int _spaceCounter;

        internal StringBuilder _sequenceBuilder = new StringBuilder();

        public int LineIntendationThreshold { get; set; }
        public string Sequence { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == '\t')
            {
                if (_spaceCounter > 0)
                    throw new GDInvalidReadingStateException();

                _spaceCounter = 0;

                _sequenceBuilder.Append(c);
                _lineIntendation++;
                return;
            }

            if (c == ' ' && state.Settings.ReadFourSpacesAsIntendation)
            {
                _spaceCounter++;
                _sequenceBuilder.Append(c);

                if (_spaceCounter == 4)
                {
                    _spaceCounter = 0;
                    _lineIntendation++;
                }

                return;
            }

            Sequence = _sequenceBuilder.ToString();
            LineIntendationThreshold = _lineIntendation;

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
        public void Update()
        {
            var p = Parent;
            int intendation = 0;

            while (p != null)
            {
                if (p is GDIntendedNode)
                    intendation++;

                p = p.Parent;
            }

            LineIntendationThreshold = intendation;
            Sequence = new string('\t', intendation);
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}
