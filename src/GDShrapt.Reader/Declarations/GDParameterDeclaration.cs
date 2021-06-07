namespace GDShrapt.Reader
{
    public sealed class GDParameterDeclaration : GDNode
    {
        public GDIdentifier Identifier { get; set; }
        public GDType Type { get; set; }

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

            if (Type == null && c == ':')
            {
                state.PushNode(Type = new GDType());
            }

            state.PopNode();

            if (c == ')')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            // Ignore
            // TODO: if needs handling
        }

        public override string ToString()
        {
            if (Type == null)
                return $"{Identifier}";
            else
                return $"{Identifier} : {Type}";
        }
    }
}