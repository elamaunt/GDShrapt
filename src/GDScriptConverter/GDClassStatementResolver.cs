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

            GDStatement statement = null;

            switch (Sequence)
            {
                case "tool":
                    statement = new GDToolAtribute();
                    break;
                case "func":
                    statement = new GDMethodDeclaration();
                    break;
                case "var":
                    statement = new GDVariableDeclaration();
                    break;
                default:
                    statement = new GDInvalidStatement(Sequence);
                    break;
            }

            GDClass.Statements.Add(statement);
            state.PushNode(statement);
        }
    }
}