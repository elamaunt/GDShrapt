namespace GDScriptConverter
{
    public class GDParameterDeclaration : GDNode
    {
        public GDIdentifier Identifier { get; set; }
        public GDType Type { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == '(')
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                state.HandleChar(c);
                return;
            }

            if (Type == null && c == ':')
            {
                state.PushNode(Type = new GDType());
            }

            if (c == ')')
            {
                state.PopNode();
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {

        }
    }
}