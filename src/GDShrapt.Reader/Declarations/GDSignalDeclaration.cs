namespace GDShrapt.Reader
{
    public sealed class GDSignalDeclaration : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }

        public GDParametersDeclaration Parameters { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.Push(Identifier = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            if (Parameters == null)
            {
                state.Push(Parameters = new GDParametersDeclaration());
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"signal {Identifier}{Parameters}";
        }
    }
}
