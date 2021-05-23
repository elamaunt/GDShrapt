using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDExressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;

        Stack<GDExpression> _expressionsStack = new Stack<GDExpression>();

        GDExpression Last => _expressionsStack.PeekOrDefault();

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
                        case GDMemberOperatorExpression expr2:
                            {
                                PushAndPeek(state, new GDCallExression()
                                {
                                    CallerExpression = expr ?? expr2
                                });
                                break;
                            }
                        default:
                            break;
                    }

                    break;
                case ')':
                    break;
                default:
                    break;
            }

            if (c == '.')
            {
                return;
            }

            if (c=='"')
            {
                return;
            }

            if (c == '>')
            {
                
            }

            if (c == '<')
            {
                return;
            }

            if (c == '=')
            {
                return;
            }

            if (c == '!')
            {
                return;
            }

            if (c == '(')
            {

                return;
            }

            if (c == ')')
            {
                return;
            }



            state.PushNode(_expressionsStack.PushAndPeek(new GDIdentifierExpression()));

            // TODO: another expressions
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new NotImplementedException();
        }

        private void PushAndPeek(GDReadingState state, GDExpression node)
        {
            state.PushNode(_expressionsStack.PushAndPeek(node));
        }
    }
}