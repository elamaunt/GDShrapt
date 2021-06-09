using System;

namespace GDShrapt.Reader
{
    internal class GDExpressionResolver : GDResolver
    {
        GDExpression _expression;

        bool _ifExpressionChecked;
        bool _isIfExpressionNext;
        bool _isCompleted;

        public GDExpressionResolver(ITokensContainer owner)
            : base(owner)
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',' || c == '}' || c == ')' || c == ']' || c == ':' || c ==';')
            {
                if (!CheckKeywords(state))
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
                if (CheckKeywords(state))
                {
                    state.PassChar(' ');
                    state.PassChar(c);
                    return;
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
                    state.Push(new GDStaticKeywordResolver(this));
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

        private bool CheckKeywords(GDReadingState state)
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
                        }
                        return true;
                    case "not":
                        PushAndSave(state, new GDSingleOperatorExpression()
                        {
                            OperatorType = GDSingleOperatorType.Not2
                        });
                        return true;
                    case "var":
                        PushAndSave(state, new GDVariableDeclarationExpression());
                        return true;
                    case "pass":
                        PushAndSave(state, new GDPassExpression());
                        return true;
                    case "continue":
                        PushAndSave(state, new GDContinueExpression());
                        return true;
                    case "return":
                        PushAndSave(state, new GDReturnExpression());
                        return true;
                    case "_":
                        PushAndSave(state, new GDMatchDefaultOperatorExpression());
                        return true;
                    case "false":
                        PushAndSave(state, new GDBoolExpression());
                        return true;
                    case "true":
                        PushAndSave(state, new GDBoolExpression()
                        {
                            Value = true
                        });
                        return true;
                    default:
                        break;
                }
            }

            return false;
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!CheckKeywords(state))
                CompleteExpression(state);
            state.PassLineFinish();
        }

        private void CompleteExpression(GDReadingState state)
        {
            if (_isCompleted)
                return;

            _isCompleted = true;

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

                Append(last.RebuildRootOfPriorityIfNeeded());
            }

            state.Pop();
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            state.Push(_expression = node);
        }
    }
}