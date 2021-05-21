namespace GDScriptConverter
{
    public class GDClassStatementResolver : GDCharSequenceNode
    {
        public GDClass GDClass { get; }

        public GDClassStatementResolver(GDClass gDClass)
        {
            GDClass = gDClass;
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != ' ';
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
        }

        protected override void CompleteSequence(GDReadingState state)
        {
            base.CompleteSequence(state);

            switch (Sequence)
            {
                case "tool":
                    break;
                case "func":
                    break;
                case "var":
                    state.PushNode(GDClass.AppendStatement(new GDVariableDeclaration()));
                    break;
                default:
                    break;
            }
        }
    }
}