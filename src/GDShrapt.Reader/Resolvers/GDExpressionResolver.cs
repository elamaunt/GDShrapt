using System;

namespace GDShrapt.Reader
{
    internal class GDExpressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;
        GDExpression _expression;
        public GDExpressionResolver(Action<GDExpression> handler)
        {
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',' || c == '}' || c == ')' || c == ']' || c == ':')
            {
                CompleteExpression(state);
                state.PassChar(c);
                return;
            }

            if (_expression == null)
            {
                if (c == '[')
                {
                    PushAndSave(state, new GDArrayInitializerExpression());
                    return;
                }

                if (c == '{')
                {
                    PushAndSave(state, new GDDictionaryInitializerExpression());
                    return;
                }

                if (c == '(')
                {
                    PushAndSave(state, new GDBracketExpression());
                    return;
                }

                if (c == '\"' || c == '\'')
                {
                    PushAndSave(state, new GDStringExpression());
                    state.PassChar(c);
                    return;
                }

                if (char.IsDigit(c))
                {
                    PushAndSave(state, new GDNumberExpression());
                    state.PassChar(c);
                    return;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    PushAndSave(state, new GDIdentifierExpression());
                    state.PassChar(c);
                    return;
                }

                if (c == '@')
                {
                    PushAndSave(state, new GDNodePathExpression());
                    return;
                }

                if (c == '$')
                {
                    PushAndSave(state, new GDGetNodeExpression());
                    return;
                }

                if (c == '.')
                {
                    PushAndSave(state, new GDMemberOperatorExpression());
                    return;
                }

                if (c == '-' || c == '!' || c == '~')
                {
                    PushAndSave(state, new GDSingleOperatorExpression());
                    state.PassChar(c);
                    return;
                }
            }
            else
            {
                if (_expression is GDIdentifierExpression identifierExpr)
                {
                    switch (identifierExpr.Identifier?.Sequence)
                    {
                        case "not":
                            PushAndSave(state, new GDSingleOperatorExpression()
                            {
                                OperatorType = GDSingleOperatorType.Not2
                            });
                            state.PassChar(c);
                            return;

                        case "var":
                            PushAndSave(state, new GDVariableDeclarationExpression());
                            state.PassChar(c);
                            return;
                        case "pass":
                            PushAndSave(state, new GDPassExpression());
                            state.PassChar(c);
                            return;
                        case "return":
                            PushAndSave(state, new GDReturnExpression());
                            state.PassChar(c);
                            return; 
                        default:
                            break;
                    }
                }

                if (c == '(')
                {
                    PushAndSave(state, new GDCallExression()
                    {
                        CallerExpression = _expression
                    });
                    return;
                }

                if (c == '[')
                {
                    PushAndSave(state, new GDIndexerExression()
                    {
                        CallerExpression = _expression
                    });
                    state.PassChar(c);
                    return;
                }

                if (c == '.')
                {
                    PushAndSave(state, new GDMemberOperatorExpression()
                    {
                        CallerExpression = _expression
                    });
                    return;
                }

                PushAndSave(state, new GDDualOperatorExression()
                {
                    LeftExpression = _expression
                });
                state.PassChar(c);
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteExpression(state);
            state.PassLineFinish();
        }

        private void CompleteExpression(GDReadingState state)
        {
            var last = _expression;

            if (last != null)
            {
                var expr = last.RebuildOfPriorityIfNeeded();
                expr.EndLineComment = EndLineComment;
                EndLineComment = null;
                _handler(expr);
            }

            state.PopNode();
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            state.PushNode(_expression = node);
        }
    }
}