using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDIfStatement : GDStatement
    {
        private bool _expressionEnded;
        private bool _trueStatementsChecked;

        public GDExpression Condition { get; set; }
        public List<GDStatement> TrueStatements { get; } = new List<GDStatement>();
        public List<GDStatement> FalseStatements { get; } = new List<GDStatement>();

        internal GDIfStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDIfStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Condition == null)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            if (!_expressionEnded)
            {
                if (c != ':')
                    return;

                _expressionEnded = true;
                return;
            }

            if (!_trueStatementsChecked)
            {
                _trueStatementsChecked = true;
                var statement = new GDExpressionStatement(LineIntendation + 1);
                TrueStatements.Add(statement);
                state.Push(statement);
                state.PassChar(c);
                return;
            }

            // 'if' statement doesn't handle 'else' and 'elif' branches by yourself. It is managed by statement resolver.
            // Just return control flow to previous node.
            state.Pop();
            state.PassChar(c);
        }


        internal void HandleFalseStatements(GDReadingState state)
        {
            state.Push(new GDStatementResolver(this, LineIntendation + 1));
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_trueStatementsChecked)
            {
                _trueStatementsChecked = true;
                state.Push(new GDStatementResolver(this, LineIntendation + 1));
                return;
            }

            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            if (FalseStatements.Count == 0)
            {
                return $@"if {Condition}:
    {string.Join("\n\t", TrueStatements.Select(x => x.ToString()))}";
            }
            else
            {
                if (FalseStatements.Count == 1 && FalseStatements[0] is GDIfStatement statement)
                {
                    return $@"if {Condition}:
    {string.Join("\n\t", TrueStatements.Select(x => x.ToString()))}
el{statement}";
                }
                else
                {
                    return $@"if {Condition}:
    {string.Join("\n\t", TrueStatements.Select(x => x.ToString()))}
else:
    {string.Join("\n\t", FalseStatements.Select(x => x.ToString()))}";

                }
            }
        }
    }
}