namespace GDShrapt.Reader
{
    public sealed class GDComment : GDCharSequence
    {
        public new string Sequence
        {
            get => base.Sequence;
            set => base.Sequence = (value?.StartsWith("#") ?? true) ? value : "#" + value;
        }

        public GDComment()
        {
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            CompleteSequence(state);
            state.PassNewLine();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            CompleteSequence(state);
            state.PassCarriageReturnChar();
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return true;
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            HandleChar('\\', state);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDComment()
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