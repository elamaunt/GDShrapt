using System;

namespace GDSharp.Reader
{
    public class GDExpressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;
        GDExpression _expression;
        public GDExpressionResolver(Action<GDExpression> handler)
        {
            _handler = handler;
        }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',' || c == ')')
            {
                CompleteExpression(state);
                state.HandleChar(c);
                return;
            }

            if (_expression == null)
            {
                if (c == '[')
                {
                    PushAndSave(state, new GDArrayInitializerExpression());
                    return;
                }

                if (c == '(')
                {
                    PushAndSave(state, new GDBracketExpression());
                    return;
                }

                if (c == '\"')
                {
                    PushAndSave(state, new GDStringExpression());
                    return;
                }

                if (char.IsDigit(c))
                {
                    PushAndSave(state, new GDNumberExpression());
                    state.HandleChar(c);
                    return;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    PushAndSave(state, new GDIdentifierExpression());
                    state.HandleChar(c);
                    return;
                }

                if (c == '.')
                {
                    PushAndSave(state, new GDMemberOperatorExpression());
                    return;
                }

                if (c == '-' || c == '!')
                {
                    PushAndSave(state, new GDSingleOperatorExpression());
                    state.HandleChar(c);
                    return;
                }
            }
            else
            {
                if (c == '(')
                {
                    PushAndSave(state, new GDCallExression()
                    {
                        CallerExpression = _expression
                    });
                    return;
                }

                PushAndSave(state, new GDDualOperatorExression()
                {
                    LeftExpression = _expression
                });
                state.HandleChar(c);
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteExpression(state);
            state.FinishLine();
        }

        private void CompleteExpression(GDReadingState state)
        {
            var last = _expression;

            if (last != null)
                _handler(last.RebuildOfPriorityIfNeeded());

            state.PopNode();
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            state.PushNode(_expression = node);
        }
    }
}