namespace GDShrapt.Reader
{
    internal class GDExpressionResolver : GDResolver, 
        ITokenOrSkipReceiver<GDIfKeyword>
    {
        GDExpression _expression;
        GDIfKeyword _nextIfKeyword;
        GDSpace _lastSpace;

        bool _ifExpressionChecked;
        bool _isCompleted;
        readonly int _intendation;
        readonly bool _allowAssignment;

        new ITokenReceiver<GDExpression> Owner { get; }
        ITokenSkipReceiver<GDExpression> OwnerWithSkip { get; }
        INewLineReceiver NewLineReceiver { get; }
        public bool IsCompleted => _isCompleted;

        public GDExpressionResolver(ITokenOrSkipReceiver<GDExpression> owner, int intendation, INewLineReceiver newLineReceiver = null, bool allowAssignment = true)
            : base(owner)
        {
            Owner = owner;
            OwnerWithSkip = owner;
            NewLineReceiver = newLineReceiver;
            _intendation = intendation;
            _allowAssignment = allowAssignment;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_nextIfKeyword != null)
            {
                var keyword = _nextIfKeyword;
                _ifExpressionChecked = false;
                _nextIfKeyword = null;

                var expr = new GDIfExpression(_intendation, NewLineReceiver != null);
                PushAndSwap(state, expr);
                expr.Add(keyword);

                state.PassChar(c);
                return;
            }

            if (IsSpace(c))
            {
                state.Push(_lastSpace = new GDSpace());
                state.PassChar(c);
                return;
            }

            if (IsExpressionStopChar(c))
            {
                if (!CheckKeywords(state))
                    CompleteExpression(state);

                if (_lastSpace != null)
                {
                    Owner.HandleReceivedToken(_lastSpace);
                    _lastSpace = null;
                }

                state.PassChar(c);
                return;
            }

            if (_expression == null)
            {
                if (c == '[')
                {
                    PushAndSave(state, new GDArrayInitializerExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '{')
                {
                    PushAndSave(state, new GDDictionaryInitializerExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '(')
                {
                    PushAndSave(state, new GDBracketExpression(_intendation));
                    state.PassChar(c);
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

                if (c == '$')
                {
                    PushAndSave(state, new GDGetNodeExpression());
                    state.PassChar(c);
                    return;
                }

                if (c == '^')
                {
                    PushAndSave(state, new GDNodePathExpression());
                    state.PassChar(c);
                    return;
                }

                if (c == '.')
                {
                    PushAndSwap(state, new GDMemberOperatorExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '-' || c == '!' || c == '~')
                {
                    PushAndSave(state, new GDSingleOperatorExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '%')
                {
                    PushAndSave(state, new GDGetUniqueNodeExpression());
                    state.PassChar(c);
                    return;
                }
            }
            else
            {
                if (_expression is GDMethodExpression)
                {
                    CompleteExpression(state);
                    return;
                }

                if (CheckKeywords(state))
                {
                    state.PassChar(c);
                    return;
                }

                if (_expression is GDDualOperatorExpression dualOperatorExpression)
                {
                    if (dualOperatorExpression.OperatorType == GDDualOperatorType.Null)
                    {
                        // This is the end of the expression.
                        var form = dualOperatorExpression.Form;
                        var leftExpression = dualOperatorExpression.LeftExpression;

                        _expression = leftExpression;

                        CompleteExpression(state);

                        // Send all next tokens to the current reader
                        foreach (var token in form.GetAllTokensAfter(0))
                            state.PassString(token.ToString());

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

                if (c == '(')
                {
                    PushAndSwap(state, new GDCallExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '[')
                {
                    PushAndSwap(state, new GDIndexerExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '.')
                {
                    PushAndSwap(state, new GDMemberOperatorExpression(_intendation));
                    state.PassChar(c);
                    return;
                }

                if (c == '=' && !_allowAssignment)
                {
                    CompleteExpression(state);
                    state.PassChar(c);
                    return;
                }

                PushAndSwap(state, new GDDualOperatorExpression(_intendation, NewLineReceiver != null));
                state.PassChar(c);
            }
        }

        private bool CheckKeywords(GDReadingState state)
        {
            if (_expression is GDMethodExpression)
            {
                return false;
            }

            if (_expression is GDIdentifierExpression identifierExpr)
            {
                var s = identifierExpr.Identifier?.Sequence;

                switch (s)
                {
                    case "if":
                    case "else":
                    case "when":
                        {
                            _expression = null;

                            var space = _lastSpace;
                            _lastSpace = null;

                            CompleteExpression(state);

                            for (int i = 0; i < s.Length; i++)
                                state.PassChar(s[i]);

                            if (space != null)
                                state.PassString(space.Sequence);
                        }
                        return true;
                    case "setget":
                        {
                            _expression = null;

                            if (_lastSpace != null)
                            {
                                Owner.HandleReceivedToken(_lastSpace);
                                _lastSpace = null;
                            }

                            CompleteExpression(state);

                            for (int i = 0; i < s.Length; i++)
                                state.PassChar(s[i]);
                        }
                        return true;
                    case "not":
                        {
                            var e = new GDSingleOperatorExpression(_intendation);
                            e.Add(new GDSingleOperator() { OperatorType = GDSingleOperatorType.Not2 });
                            PushAndSave(state, e);
                            return true;
                        }
                    case "var":
                        {
                            var e = new GDMatchCaseVariableExpression();
                            e.Add(new GDVarKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "pass":
                        {
                            var e = new GDPassExpression();
                            e.Add(new GDPassKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "break":
                        {
                            var e = new GDBreakExpression();
                            e.Add(new GDBreakKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "continue":
                        {
                            var e = new GDContinueExpression();
                            e.Add(new GDContinueKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "return":
                        {
                            var e = new GDReturnExpression(_intendation);
                            e.Add(new GDReturnKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "_":
                        {
                            var e = new GDMatchDefaultOperatorExpression();
                            e.Add(new GDDefaultToken());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "false":
                        {
                            var e = new GDBoolExpression();
                            e.Add(new GDFalseKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "true":
                        {
                            var e = new GDBoolExpression();
                            e.Add(new GDTrueKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "yield":
                        {
                            var e = new GDYieldExpression(_intendation);
                            e.Add(new GDYieldKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "await":
                        {
                            var e = new GDAwaitExpression(_intendation);
                            e.Add(new GDAwaitKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    case "func":
                        {
                            var e = new GDMethodExpression(_intendation);
                            e.Add(new GDFuncKeyword());
                            PushAndSave(state, e);
                            return true;
                        }
                    default:
                        break;
                }
            }

            return false;
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (NewLineReceiver != null)
            {
                if (_expression != null)
                {
                    PushAndSwap(state, new GDDualOperatorExpression(_intendation, true));
                    state.PassNewLine();
                }
                else
                {
                    if (_lastSpace != null)
                    {
                        NewLineReceiver.HandleReceivedToken(_lastSpace);
                        _lastSpace = null;
                    }

                    NewLineReceiver.HandleReceivedToken(new GDNewLine());
                }
            }
            else
            {
                if (!CheckKeywords(state))
                    CompleteExpression(state);
                state.PassNewLine();
            }
            
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (NewLineReceiver != null)
            {
                if (_expression != null)
                {
                    PushAndSwap(state, new GDDualOperatorExpression(_intendation, true));
                    state.PassSharpChar();
                }
                else
                {
                    if (_lastSpace != null)
                    {
                        NewLineReceiver.HandleReceivedToken(_lastSpace);
                        _lastSpace = null;
                    }

                    NewLineReceiver.HandleReceivedToken(state.PushAndPass(new GDComment(), '#'));
                }
            }
            else
            {
                if (!CheckKeywords(state))
                    CompleteExpression(state);
                state.PassSharpChar();
            }
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            if (NewLineReceiver != null)
            {
                if (_expression != null)
                {
                    PushAndSwap(state, new GDDualOperatorExpression(_intendation, true));
                    state.PassLeftSlashChar();
                }
                else
                {
                    if (_lastSpace != null)
                    {
                        NewLineReceiver.HandleReceivedToken(_lastSpace);
                        _lastSpace = null;
                    }

                    NewLineReceiver.HandleReceivedToken(state.PushAndPass(new GDMultiLineSplitToken(), '\\'));
                }
            }
            else
            {
                if (!CheckKeywords(state))
                    CompleteExpression(state);
                state.PassLeftSlashChar();
            }
        }

        private void CompleteExpression(GDReadingState state)
        {
            if (_isCompleted)
                return;

            _isCompleted = true;

            var last = _expression;

            _expression = null;

            state.Pop();

            if (last != null)
            {
                // Handle negative number from Negate operator and GDNumberExpression. It must be zero tokens between negate and the number
                if (last is GDSingleOperatorExpression operatorExpression &&
                    operatorExpression.OperatorType == GDSingleOperatorType.Negate &&
                    operatorExpression.Form.CountTokensBetween(0, 1) == 0 &&
                    operatorExpression.TargetExpression is GDNumberExpression numberExpression)
                {
                    numberExpression.Number.Negate();
                    last = operatorExpression.TargetExpression;

                    Owner.HandleReceivedToken(last.RebuildRootOfPriorityIfNeeded());

                    // Send all tokens after Single operator to current reader
                    foreach (var token in operatorExpression.Form.GetAllTokensAfter(1))
                        state.PassString(token.ToString());
                }
                else
                {
                    Owner.HandleReceivedToken(last.RebuildRootOfPriorityIfNeeded());
                }
            }
            else
                OwnerWithSkip?.HandleReceivedTokenSkip();

            if (_lastSpace != null)
            {
                Owner.HandleReceivedToken(_lastSpace);
                _lastSpace = null;
            }
        }

        private void PushAndSwap<T>(GDReadingState state, T node)
            where T: GDExpression, ITokenOrSkipReceiver<GDExpression>
        {
            if (_expression != null)
                node.HandleReceivedToken(_expression);
            else
                node.HandleReceivedTokenSkip();

            if (_lastSpace != null)
            {
                node.HandleReceivedToken(_lastSpace);
                _lastSpace = null;
            }

            state.Push(_expression = node);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (!CheckKeywords(state))
                CompleteExpression(state);
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            if (_lastSpace != null)
            {
                node.Add(_lastSpace);
                _lastSpace = null;
            }

            state.Push(_expression = node);
        }

        public void HandleReceivedToken(GDIfKeyword token)
        {
            _nextIfKeyword = token;
        }

        public void HandleReceivedTokenSkip()
        {
            // Nothing
        }

        public void HandleReceivedToken(GDComment token)
        {
            Owner.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDAttribute token)
        {
            Owner.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDSpace token)
        {
            Owner.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDInvalidToken token)
        {
            Owner.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDMultiLineSplitToken token)
        {
            Owner.HandleReceivedToken(token);
        }
    }
}