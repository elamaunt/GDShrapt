using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal class GDExpressionResolver : GDResolver,
        ITokenOrSkipReceiver<GDIfKeyword>,
        ITokenOrSkipReceiver<GDDoubleDot>
    {
        GDExpression _expression;
        GDIfKeyword _nextIfKeyword;
        GDDoubleDot _nextDoubleDot;
        bool _singleDotDetected;

        List<GDCharSequence> _lastSplitTokens;

        bool _ifExpressionChecked;
        bool _isCompleted;
        readonly int _intendation;
        readonly bool _allowAssignment;
        readonly bool _allowNewLines;

        new ITokenReceiver<GDExpression> Owner { get; }
        ITokenSkipReceiver<GDExpression> OwnerWithSkip { get; }
        INewLineReceiver NewLineReceiver { get; }
        public bool IsCompleted => _isCompleted;

        public GDExpressionResolver(ITokenOrSkipReceiver<GDExpression> owner, int intendation, INewLineReceiver newLineReceiver = null, bool allowAssignment = true, bool allowNewLines = false)
            : base(owner)
        {
            Owner = owner;
            OwnerWithSkip = owner;
            NewLineReceiver = newLineReceiver;
            _intendation = intendation;
            _allowAssignment = allowAssignment;
            _allowNewLines = allowNewLines;
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

            if (_nextDoubleDot != null)
            {
                var doubleDot = _nextDoubleDot;
                _nextDoubleDot = null;

                var expr = new GDRestExpression();
                ((ITokenReceiver<GDDoubleDot>)expr).HandleReceivedToken(doubleDot);
                PushAndSave(state, expr);

                state.PassChar(c);
                return;
            }

            if (IsSpace(c))
            {

                state.Push(AddLastSplitToken(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (IsExpressionStopChar(c))
            {
                if (_isCompleted)
                {
                    state.PopAndPass(c);
                    return;
                }

                if (!CheckKeywords(state))
                    CompleteExpression(state);

                FlushSplitTokens(state);

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
                    if (_singleDotDetected)
                    {
                        // We already checked for ".." and it was just "." - create member operator
                        _singleDotDetected = false;
                        PushAndSwap(state, new GDMemberOperatorExpression(_intendation));
                        state.PassChar(c);
                        return;
                    }

                    // Could be ".." (rest operator) or just "." (member access without caller)
                    // Try to resolve as ".." first
                    state.PushAndPass(new GDSequenceTokenResolver<GDDoubleDot>(this), c);
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

                if (c == '&')
                {
                    // StringName literal: &"name" or &'name'
                    PushAndSave(state, new GDStringNameExpression());
                    state.PassChar(c);
                    return;
                }

                FlushSplitTokens(state);

                Owner.HandleAsInvalidToken(c, state, x => c != x);
            }
            else
            {
                if (_expression is GDIdentifierExpression idExpr
                    && idExpr.Identifier?.Sequence == "r"
                    && (c == '"' || c == '\''))
                {
                    _expression = null;
                    var rawExpr = new GDRawStringExpression();
                    rawExpr.RawPrefix = new GDRawStringPrefix();
                    rawExpr.TypedForm.State = GDRawStringExpression.State.String;
                    PushAndSave(state, rawExpr);
                    state.PassChar(c);
                    return;
                }

                if (_expression is GDMethodExpression)
                {
                    if (c != '.' && c != '(' && c != '[')
                    {
                        CompleteExpression(state);
                        FlushSplitTokens(state);
                        state.PassChar(c);
                        return;
                    }
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
                            PassToken(state, token);

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

                PushAndSwap(state, new GDDualOperatorExpression(_intendation, NewLineReceiver != null || _allowNewLines));
                state.PassChar(c);
            }
        }

        private T AddLastSplitToken<T>(T splitToken) where T : GDCharSequence
        {
            if (_lastSplitTokens == null)
                _lastSplitTokens = new List<GDCharSequence>();

            _lastSplitTokens.Add(splitToken);
            return splitToken;
        }

        /// <summary>
        /// Отправляет токен в ITokenReceiver (Owner или NewLineReceiver)
        /// </summary>
        private void SendTokenToReceiver(ITokenReceiver receiver, GDCharSequence token)
        {
            if (token is GDSpace space)
                receiver.HandleReceivedToken(space);
            else if (token is GDMultiLineSplitToken multiLine)
                receiver.HandleReceivedToken(multiLine);
        }

        /// <summary>
        /// Сливает накопленные токены в Owner (выражение продолжается)
        /// </summary>
        private void FlushSplitTokensToOwner()
        {
            if (_lastSplitTokens == null)
                return;

            foreach (var token in _lastSplitTokens)
                SendTokenToReceiver(Owner, token);

            _lastSplitTokens.Clear();
        }

        /// <summary>
        /// Сливает накопленные токены в state через PassChar (выражение завершено)
        /// </summary>
        private void FlushSplitTokensToState(GDReadingState state)
        {
            if (_lastSplitTokens == null)
                return;

            foreach (var token in _lastSplitTokens)
            {
                var seq = token.Sequence;
                for (int i = 0; i < seq.Length; i++)
                    state.PassChar(seq[i]);
            }

            _lastSplitTokens.Clear();
        }

        /// <summary>
        /// Сливает токены: в Owner если не завершен, иначе в state
        /// </summary>
        private void FlushSplitTokens(GDReadingState state)
        {
            if (_lastSplitTokens == null)
                return;

            if (!Owner.IsCompleted)
                FlushSplitTokensToOwner();
            else
                FlushSplitTokensToState(state);
        }

        /// <summary>
        /// Сливает токены в expression node (для PushAndSwap/PushAndSave)
        /// </summary>
        private void FlushSplitTokensToNode(GDExpression node)
        {
            if (_lastSplitTokens == null)
                return;

            // Используем ITokenReceiver для вызова правильных перегрузок
            ITokenReceiver receiver = node;
            foreach (var token in _lastSplitTokens)
                SendTokenToReceiver(receiver, token);

            _lastSplitTokens.Clear();
        }

        /// <summary>
        /// Сливает токены в NewLineReceiver
        /// </summary>
        private void FlushSplitTokensToNewLineReceiver()
        {
            if (_lastSplitTokens == null || NewLineReceiver == null)
                return;

            foreach (var token in _lastSplitTokens)
                SendTokenToReceiver(NewLineReceiver, token);

            _lastSplitTokens.Clear();
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

                            // Сохраняем токены для передачи в state после CompleteExpression
                            var savedTokens = _lastSplitTokens;
                            _lastSplitTokens = null;

                            CompleteExpression(state);

                            for (int i = 0; i < s.Length; i++)
                                state.PassChar(s[i]);

                            if (savedTokens != null)
                            {
                                foreach (var token in savedTokens)
                                    state.PassString(token.Sequence);
                            }
                        }
                        return true;
                    case "setget":
                        {
                            _expression = null;

                            if (!Owner.IsCompleted)
                                FlushSplitTokensToOwner();

                            CompleteExpression(state);

                            for (int i = 0; i < s.Length; i++)
                                state.PassChar(s[i]);

                            if (Owner.IsCompleted)
                                FlushSplitTokensToState(state);
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
                    FlushSplitTokensToNewLineReceiver();
                    NewLineReceiver.HandleReceivedToken(new GDNewLine());
                }
            }
            else if (_expression != null && _allowNewLines)
            {
                PushAndSwap(state, new GDDualOperatorExpression(_intendation, true));
                state.PassNewLine();
            }
            else
            {
                if (!CheckKeywords(state))
                    CompleteExpression(state);
                state.PassNewLine();
            }
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            // CR handling mirrors NL handling - complete expression first, then pass CR
            if (NewLineReceiver != null)
            {
                if (_expression != null)
                {
                    PushAndSwap(state, new GDDualOperatorExpression(_intendation, true));
                    state.PassCarriageReturnChar();
                }
                else
                {
                    FlushSplitTokensToNewLineReceiver();
                    // Pass CR to NewLineReceiver as a token
                    ((ITokenReceiver)NewLineReceiver).HandleReceivedToken(new GDCarriageReturnToken());
                }
            }
            else
            {
                if (!CheckKeywords(state))
                    CompleteExpression(state);
                state.PassCarriageReturnChar();
            }
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (NewLineReceiver != null)
            {
                if (!NewLineReceiver.IsCompleted)
                {
                    if (_expression != null)
                    {
                        PushAndSwap(state, new GDDualOperatorExpression(_intendation, true));
                        state.PassSharpChar();
                    }
                    else
                    {
                        FlushSplitTokensToNewLineReceiver();
                        NewLineReceiver.HandleReceivedToken(state.PushAndPass(new GDComment(), '#'));
                    }
                }
                else
                {
                    state.Pop();
                    state.Pop();

                    FlushSplitTokensToState(state);
                    state.PassSharpChar();
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
                    FlushSplitTokensToNewLineReceiver();
                    NewLineReceiver.HandleReceivedToken(state.PushAndPass(new GDMultiLineSplitToken(), '\\'));
                }
            }
            else
            {
                state.Push(AddLastSplitToken(new GDMultiLineSplitToken()));
                state.PassLeftSlashChar();
                //if (!CheckKeywords(state))
                //    CompleteExpression(state);
                //state.PassLeftSlashChar();
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

                    // Send all tokens after Single operator to the current reader
                    foreach (var token in operatorExpression.Form.GetAllTokensAfter(1))
                        PassToken(state, token);
                }
                else if (last is GDDualOperatorExpression dualOperatorExpression &&
                    dualOperatorExpression.RightExpression == null &&
                    dualOperatorExpression.Operator == null)
                {
                    last = dualOperatorExpression.LeftExpression;

                    Owner.HandleReceivedToken(last.RebuildRootOfPriorityIfNeeded());

                    // Send all tokens after Left expression to the current reader
                    foreach (var token in dualOperatorExpression.Form.GetAllTokensAfter(0))
                        PassToken(state, token);
                }
                else
                {
                    Owner.HandleReceivedToken(last.RebuildRootOfPriorityIfNeeded());
                }
            }
            else
                OwnerWithSkip?.HandleReceivedTokenSkip();

            FlushSplitTokens(state);
        }

        /// <summary>
        /// Passes a token to the state, handling special tokens like CR that have empty ToString().
        /// </summary>
        private static void PassToken(GDReadingState state, GDSyntaxToken token)
        {
            if (token is GDCarriageReturnToken)
            {
                state.PassCarriageReturnChar();
            }
            else if (token is GDNewLine)
            {
                state.PassNewLine();
            }
            else
            {
                state.PassString(token.ToString());
            }
        }

        private void PushAndSwap<T>(GDReadingState state, T node)
            where T: GDExpression, ITokenOrSkipReceiver<GDExpression>
        {
            if (_expression != null)
                node.HandleReceivedToken(_expression);
            else
                node.HandleReceivedTokenSkip();

            FlushSplitTokensToNode(node);

            state.Push(_expression = node);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (!CheckKeywords(state))
                CompleteExpression(state);
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            FlushSplitTokensToNode(node);
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

        public void HandleReceivedToken(GDCarriageReturnToken token)
        {
            Owner.HandleReceivedToken(token);
        }

        void ITokenReceiver<GDDoubleDot>.HandleReceivedToken(GDDoubleDot token)
        {
            _nextDoubleDot = token;
        }

        void ITokenSkipReceiver<GDDoubleDot>.HandleReceivedTokenSkip()
        {
            // ".." was not matched, it's just a single "." - mark it so HandleChar creates member operator
            // The first "." character will be re-passed by the GDSequenceResolver
            _singleDotDetected = true;
        }
    }
}