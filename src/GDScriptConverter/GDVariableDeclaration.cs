namespace GDScriptConverter
{
    public class GDVariableDeclaration : GDStatement
    {
        public GDIdentifier Identifier { get; set; }
        public GDType Type { get; set; }
        public GDExpression Initializer { get; set; }

        public override void HandleChar(char c, GDReadingState state)
        {
            if (c == ' ' || c == '\t')
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                return;
            }

            if (Type == null && c == ':')
            {
                state.PushNode(Type = new GDType());
                return;
            }

            state.PushNode(new 
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }
    }
}