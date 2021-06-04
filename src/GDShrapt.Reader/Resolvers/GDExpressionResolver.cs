using System;

namespace GDShrapt.Reader
{
    internal class GDExpressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;
        GDExpression _expression;

        bool _ifExpressionChecked;
        bool _isIfExpressionNext;

        public GDExpressionResolver(Action<GDExpression> handler)
        {
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',' || c == '}' || c == ')' || c == ']' || c == ':' || c ==';')
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
                    var s = identifierExpr.Identifier?.Sequence;
                    switch (s)
                    {
                        case "if":
                        case "else":
                        case "setget":
                            {
                                _expression = null;
                                CompleteExpression(state);

                                for (int i = 0; i < s.Length; i++)
                                    state.PassChar(s[i]);

                                state.PassChar(' ');
                                state.PassChar(c);
                            }
                            return;
                        case "not":
                            PushAndSave(state, new GDSingleOperatorExpression()
                            {
                                OperatorType = GDSingleOperatorType.Not2
                            });
                            state.PassChar(' ');
                            state.PassChar(c);
                            return;
                        case "var":
                            PushAndSave(state, new GDVariableDeclarationExpression());
                            state.PassChar(' ');
                            state.PassChar(c);
                            return;
                        case "pass":
                            PushAndSave(state, new GDPassExpression());
                            state.PassChar(' ');
                            state.PassChar(c);
                            return;
                        case "continue":
                            PushAndSave(state, new GDContinueExpression());
                            state.PassChar(' ');
                            state.PassChar(c);
                            return;
                        case "return":
                            PushAndSave(state, new GDReturnExpression());
                            state.PassChar(' ');
                            state.PassChar(c);
                            return; 
                        
                        default:
                            break;
                    }
                }

                if (_expression is GDDualOperatorExression dualOperatorExpression)
                {
                    if (dualOperatorExpression.OperatorType == GDDualOperatorType.Unknown && dualOperatorExpression.RightExpression == null)
                    {
                        _expression = dualOperatorExpression.LeftExpression;
                        CompleteExpression(state);
                        state.PassChar(c);
                        return;
                    }
                }

                if (c == 'i' && !_ifExpressionChecked)
                {
                    _ifExpressionChecked = true;
                    state.PushNode(new GDStaticKeywordResolver("if ", result => _isIfExpressionNext = result));
                    state.PassChar(c);
                    return;
                }

                if (_isIfExpressionNext)
                {
                    _ifExpressionChecked = false;
                    _isIfExpressionNext = false;
                    PushAndSave(state, new GDIfExpression()
                    {
                        TrueExpression = _expression
                    });
                    state.PassChar(c);
                    return;
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
                // Handle negative number from Negate operator and GDNumberExpression
                if (last is GDSingleOperatorExpression operatorExpression && 
                    operatorExpression.OperatorType == GDSingleOperatorType.Negate &&
                    operatorExpression.TargetExpression is GDNumberExpression numberExpression)
                {
                    numberExpression.Number.Negate();
                    last = operatorExpression.TargetExpression;
                }

                var expr = last.RebuildRootOfPriorityIfNeeded();
                
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