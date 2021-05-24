using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDExressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;

        Stack<GDExpression> _expressionsStack = new Stack<GDExpression>();

        GDExpression Last => _expressionsStack.PeekOrDefault();

        //GDIdentifier _nextIdentifier;

        public GDExressionResolver(Action<GDExpression> handler)
        {
            _handler = handler;
        }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (char.IsDigit(c))
            {
                state.PushNode(new GDNumberExpression());
                state.HandleChar(c);
                return;
            }

            switch (c)
            {
                case '.':
                    PushAndPeek(state, new GDMemberOperatorExpression());
                    return;
                case '"':
                    PushAndPeek(state, new GDStringExpression());
                    return;
                case '>':
                case '<':
                case '=':
                case '-':
                case '+':
                case '/':
                case '*':
                case '!':
                    PushAndPeek(state, new GDOperatorExression());
                    state.HandleChar(c);
                    return;
                case '(':
                    switch (Last)
                    {
                        case GDIdentifierExpression expr:
                            {
                                _expressionsStack.Pop();
                                PushAndPeek(state, new GDCallExression() { CallerExpression = expr });
                                return;
                            }
                        case GDMemberOperatorExpression expr2:
                            {
                                _expressionsStack.Pop();
                                PushAndPeek(state, new GDCallExression() { CallerExpression = expr2 });
                                return;
                            }
                        default:
                            _expressionsStack.Push(new GDBracketExpression());
                            return;
                    }
                case ')':
                    CompleteExpression(state);
                    state.HandleChar(c);
                    return;
                default:
                    break;
            }

            // TODO: preresolve language keywords
            /*if (_nextIdentifier != null)
            {

            }
            else
            {

            }*/

            PushAndPeek(state, new GDIdentifierExpression());
            state.HandleChar(c);
            // TODO: another expressions
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteExpression(state);
        }

        private void CompleteExpression(GDReadingState state)
        {
            var last = Last;

            if (last != null)
                _handler(last);

            state.PopNode();
        }

        private void PushAndPeek(GDReadingState state, GDExpression node)
        {
            state.PushNode(_expressionsStack.PushAndPeek(node));
        }
    }
}