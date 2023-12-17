using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDStringPart : GDLiteralToken
    {
        readonly bool _allowMultiline;
        readonly GDStringBoundingChar _boundingChar;
        readonly StringBuilder _stringBuilder = new StringBuilder();

        string _sequence;
        public override string Sequence
        {
            get => _sequence;
            set => _sequence = value;
        }

        public GDStringPart()
        {
        }

        internal GDStringPart(bool allowMultiline, GDStringBoundingChar boundingChar)
        {
            _allowMultiline = allowMultiline;
            _boundingChar = boundingChar;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
           
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_allowMultiline)
            {
                HandleChar('\n', state);
            }
            else
            {
                Sequence = _stringBuilder.ToString();
                state.Pop();
                state.PassNewLine();
            }
        }

        /*public char GetBoundingChar()
        {
            switch (BoundingChar)
            {
                case GDStringBoundingChar.SingleQuotas:
                    return '\'';
                case GDStringBoundingChar.DoubleQuotas:
                    return '"';
                default:
                    throw new NotSupportedException();
            }
        }*/

        /*public static implicit operator GDString(string value)
        {
            return new GDString()
            {
                Sequence = value
            };
        }*/

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            HandleChar('\\', state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            Sequence = _stringBuilder.ToString();
            base.ForceComplete(state);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDStringPart()
            {
                Sequence = Sequence
            };
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}