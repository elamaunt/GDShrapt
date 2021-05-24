using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDStatementResolver : GDCharSequenceNode
    {
        public List<GDStatement> List { get; }

        public GDStatementResolver(List<GDStatement> list)
        {
            List = list;
        }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (SequenceBuilder?.Length == 0 && (char.IsDigit(c) || c == '.' || c == '(' ))
            {
                var statement = new GDExpressionStatement();
                List.Add(statement);
                state.PushNode(statement);
                state.HandleChar(c);
            }
            else
                base.HandleChar(c, state);
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            if (Sequence == null)
                return;
            CompleteSequence(state);
        }

        protected override void CompleteSequence(GDReadingState state)
        {
            base.CompleteSequence(state);

            GDStatement statement = null;

            switch (Sequence)
            {
                case "if":
                    statement = new GDIfStatement();
                    break;
                case "for":
                    statement = new GDForStatement();
                    break;
                case "while":
                    statement = new GDWhileStatement();
                    break;
                case "match":
                    statement = new GDMatchStatement();
                    break;
                case "yield":
                    statement = new GDYieldStatement();
                    break;
                case "var":
                    statement = new GDVariableDeclarationStatement();
                    break;
                case "return":
                    statement = new GDReturnStatement();
                    break;
                case "pass":
                    statement = new GDPassStatement();
                    break;


                //case var s when Sequence.StartsWith('.'):


                //    break;
                default:
                    break;
            }




            List.Add(statement);
            state.PushNode(statement);
        }
    }
}