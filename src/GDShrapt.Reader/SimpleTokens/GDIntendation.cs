using System.Text;

namespace GDShrapt.Reader
{
    public class GDIntendation : GDSimpleSyntaxToken
    {
        int _lineIntendation;
        int _spaceCounter;

        public int LineIntendationThreshold { get; set; }

        StringBuilder _sequenceBuilder = new StringBuilder();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == '\t')
            {
                if (_spaceCounter > 0)
                    throw new GDInvalidReadingStateException();

                _spaceCounter = 0;

                _sequenceBuilder.Append('\t');
                _lineIntendation++;
                return;
            }

            if (c == ' ' && state.Settings.ConvertFourSpacesIntoTabs)
            {
                _spaceCounter++;

                if (_spaceCounter == 4)
                {
                    _spaceCounter = 0;
                    _sequenceBuilder.Append("    ");
                    _lineIntendation++;
                }

                return;
            }

            if (LineIntendationThreshold != _lineIntendation)
                throw new GDInvalidReadingStateException();

            state.Pop();
            state.PassChar(c);
        }

        public override string ToString()
        {
            if (_sequenceBuilder.Length > 0)
                return _sequenceBuilder.ToString();

            var builder = new StringBuilder();

            for (int i = 0; i < LineIntendationThreshold; i++)
                builder.Append('\t');

            return builder.ToString();
        }
    }
}
