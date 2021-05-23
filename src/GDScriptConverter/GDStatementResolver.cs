using System;
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
                    break;
                default:
                    break;
            }

            List.Add(statement);
            state.PushNode(statement);
        }
    }
}