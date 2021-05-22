namespace GDScriptConverter
{
    public class GDMethodStatementResolver : GDCharSequenceNode
    {
        public GDMethodDeclaration Method { get; }

        public GDMethodStatementResolver(GDMethodDeclaration method)
        {
            Method = method;
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            if (Sequence == null)
                return;
            CompleteSequence(state);
        }

        protected override void CompleteSequence(GDReadingState state)
        {
            base.CompleteSequence(state);

            GDMethodStatement statement = null;

            switch (Sequence)
            {
                case "if":
                    statement = new GDConditionalStatement();
                    break;
                case "var":
                    statement = new GDVariableDeclaration
                    break;
                case "return":
                    break;
                default:
                    break;
            }
        }
    }
}