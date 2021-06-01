namespace GDShrapt.Reader
{
    public class GDSignalDeclaration : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }

        public GDParametersDeclaration Parameters { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            if (Parameters == null)
            {
                state.PushNode(Parameters = new GDParametersDeclaration());
                state.PassChar(c);
                return;
            }

            state.PopNode();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"signal {Identifier}{Parameters}";
        }
    }
}
