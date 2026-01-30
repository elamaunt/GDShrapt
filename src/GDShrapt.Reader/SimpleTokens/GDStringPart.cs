using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDStringPart : GDLiteralToken
    {
        string _sequence;
        public override string Sequence
        {
            get => _sequence;
            set => _sequence = value;
        }

        public string EscapedSequence
        {
            get
            {
                if (_sequence == null)
                    return null;

                if (_sequence.Length == 0)
                    return _sequence;

                var builder = new StringBuilder();

                for (int i = 0; i < _sequence.Length; i++)
                {
                    var ch = _sequence[i];

                    if (ch == '\\' || ch == '\'' || ch == '"')
                        builder.Append('\\');
                    builder.Append(ch);
                }

                return builder.ToString();
            }
        }

        public GDStringPart()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Nothing
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            // Nothing
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            // Nothing
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            // Nothing
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            // Nothing
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

        public override GDLiteralToken CloneWith(string stringValue)
        {
            return new GDStringPart()
            {
                Sequence = stringValue
            };
        }
    }
}