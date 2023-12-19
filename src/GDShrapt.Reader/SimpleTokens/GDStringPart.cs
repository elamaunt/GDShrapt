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