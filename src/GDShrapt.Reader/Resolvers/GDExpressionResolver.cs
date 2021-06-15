using System;

namespace GDShrapt.Reader
{
    internal class GDExpressionResolver : GDResolver, IKeywordReceiver<GDIfKeyword>, IKeywordReceiver<GDElseKeyword>
    {
        GDExpression _expression;
        GDTokensForm _expressionParentForm;
        GDIfKeyword _nextIfKeyword;

        bool _ifExpressionChecked;
        bool _isCompleted;

        new IExpressionsReceiver Owner { get; }

        public GDExpressionResolver(IExpressionsReceiver owner)
            : base(owner)
        {
            Owner = owner;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                Owner.HandleReceivedToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (IsExpressionStopChar(c))
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
                    if (dualOperatorExpression.OperatorType == GDDualOperatorType.Null && dualOperatorExpression.RightExpression == null)
                    {
                        // Save dual operators form with LeftExpression.
                        // Many operators, based on expression at start have the same form.
                        // And we wil move the form to another expression
                        _expressionParentForm = dualOperatorExpression.Form;
                        _expression = dualOperatorExpression.LeftExpression;

                        CompleteExpression(state);
                        state.PassChar(c);
                        return;
                    }
                }

                if (c == 'i' && !_ifExpressionChecked)
                {
                    _ifExpressionChecked = true;
                    state.Push(new GDKeywordResolver<GDIfKeyword>(this));
                    state.PassChar(c);
                    return;
                }

                if (_nextIfKeyword != null)
                {
                    var keyword = _nextIfKeyword;
                    _ifExpressionChecked = false;
                    _nextIfKeyword = null;

                    throw new NotImplementedException();
                    /*PushAndSave(state, new GDIfExpression()
                    {
                        TrueExpression = _expression,
                        
                    });
                    state.PassChar(c);*/
                    return;
                }

                if (c == '(')
                {
                    PushAndSwap(state, new GDCallExression());
                    return;
                }

                if (c == '[')
                {
                    PushAndSwap(state, new GDIndexerExression());
                    state.PassChar(c);
                    return;
                }

                if (c == '.')
                {
                    PushAndSwap(state, new GDMemberOperatorExpression());
                    return;
                }

                PushAndSwap(state, new GDDualOperatorExression());
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
                            OperatorType = GDSingleOperatorType.Not2,
                        });
                        return true;
                    case "var":
                        PushAndSave(state, new GDMatchCaseVariableExpression());
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

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!CheckKeywords(state))
                CompleteExpression(state);
            state.PassNewLine();
        }

        private void CompleteExpression(GDReadingState state)
        {
            if (_isCompleted)
                return;

            _isCompleted = true;

            var last = _expression;

            _expression = null;
            _expressionParentForm = null;

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

                Owner.HandleReceivedToken(last.RebuildRootOfPriorityIfNeeded());
            }
            else
                Owner.HandleReceivedExpressionSkip();

            state.Pop();
        }

        private void PushAndSwap(GDReadingState state, GDExpression node)
        {
            if (_expressionParentForm != null)
            {
                _expression.BaseForm = _expressionParentForm;
                _expressionParentForm = null;
                _expression = null;
            }

            if (_expression != null)
            {
                node.SwapLeft(_expression);

                // TODO: check all expression for state index. Are there any expressions with state index not 1
                node.Form.StateIndex = 1;
            }

            state.Push(_expression = node);
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            _expressionParentForm = null;
            state.Push(_expression = node);
        }

        public void HandleReceivedToken(GDIfKeyword token)
        {
            _nextIfKeyword = token;
        }

        public void HandleReceivedKeywordSkip()
        {
            // Nothing
        }

        public void HandleReceivedToken(GDComment token)
        {
            throw new NotImplementedException();

        }

        public void HandleReceivedToken(GDNewLine token)
        {
            throw new NotImplementedException();

        }

        public void HandleReceivedToken(GDSpace token)
        {
            throw new NotImplementedException();
        }

        public void HandleReceivedToken(GDInvalidToken token)
        {
            throw new NotImplementedException();
        }

        public void HandleReceivedToken(GDElseKeyword token)
        {
            throw new NotImplementedException();
        }

        public void HandleReceivedAbstractToken(GDSyntaxToken token)
        {
            // TODO: validate token
            Owner.HandleReceivedAbstractToken(token);
        }
    }
}