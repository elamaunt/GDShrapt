using System;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStatementsResolver : GDIntendedResolver
    {
        bool _statementResolved;
        bool _resolvedAsExpression;
        GDStatement _resolvedStatement;
        readonly bool _inExpressionContext;

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IIntendedTokenReceiver<GDStatement> Owner { get; }

        public GDStatementsResolver(IIntendedTokenReceiver<GDStatement> owner, int lineIntendation, bool inExpressionContext = false)
            : base(owner, lineIntendation)
        {
            Owner = owner;
            _inExpressionContext = inExpressionContext;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (_statementResolved)
            {
                if (IsSpace(c))
                {
                    Owner.HandleReceivedToken(state.Push(new GDSpace()));
                    state.PassChar(c);
                    return;
                }

                if (_resolvedAsExpression)
                {
                    if (_resolvedStatement.Form.IsCompleted && _resolvedStatement.Form.FirstToken == null)
                    {
                        _resolvedStatement.RemoveFromParent();
                        state.PopAndPass(c);
                        return;
                    }

                    if (c.IsExpressionStopChar())
                    {
                        // In expression context (e.g., lambda inside call), pass stop chars up
                        // This allows lambdas inside GDExpressionsList to properly terminate on comma/bracket
                        if (_inExpressionContext)
                        {
                            state.PopAndPass(c);
                            return;
                        }
                        // Otherwise treat as invalid token (top-level or malformed code)
                        Owner.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                        return;
                    }

                    // Resolving multiple expressions on the same string
                    var statement = new GDExpressionStatement(CurrentResolvedIntendationInSpaces);
                    Owner.HandleReceivedToken(statement);
                    _resolvedStatement = statement;
                    state.PushAndPass(statement, c);
                }
                else
                {
                    Owner.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                }
                return;
            }

            if (_sequenceBuilder.Length == 0)
            {
                if (IsSpace(c))
                {
                    Owner.HandleReceivedToken(state.Push(new GDSpace()));
                    state.PassChar(c);
                    return;
                }

                if (c == '@')
                {
                    SendIntendationTokensToOwner();
                    Owner.HandleReceivedToken(state.Push(new GDAttribute()));
                    state.PassChar(c);
                    return;
                }

                if (char.IsLetter(c))
                {
                    _sequenceBuilder.Append(c);
                }
                else
                {
                    if (c.IsExpressionStopChar())
                    {
                        SendIntendationTokensToOwner();
                        Owner.HandleReceivedToken(new GDInvalidToken(c.ToString()));
                    }
                    else
                    {
                        CompleteAsExpressionStatement(state);
                        state.PassChar(c);
                    }
                }
            }
            else
            {
                if (char.IsLetter(c))
                {
                    _sequenceBuilder.Append(c);
                }
                else
                {
                    CompleteAsStatement(state, _sequenceBuilder.ToString());
                    state.PassChar(c);
                }
            }
        }

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                _sequenceBuilder.Clear();
                CompleteAsStatement(state, sequence);
                state.PassNewLine();
                return;
            }

            _resolvedAsExpression = false;
            _statementResolved = false;
            ResetIntendation();
            state.PassNewLine();
        }

        internal override void HandleCarriageReturnCharAfterIntendation(GDReadingState state)
        {
            // CR handling: complete pending keyword, send CR token, but DON'T reset indentation
            // The subsequent NL will handle the line reset. CR is just a token.
            if (_sequenceBuilder.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                _sequenceBuilder.Clear();
                CompleteAsStatement(state, sequence);
            }

            // Send CR directly to owner as a token (not via PassCarriageReturnChar which would re-buffer it)
            Owner.HandleReceivedToken(new GDCarriageReturnToken());
        }

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                _sequenceBuilder.Clear();
                CompleteAsStatement(state, sequence);
                state.PassSharpChar();
                return;
            }

            Owner.HandleReceivedToken(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        internal override void HandleLeftSlashCharAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                _sequenceBuilder.Clear();
                CompleteAsStatement(state, sequence);
                state.PassLeftSlashChar();
                return;
            }

            Owner.HandleReceivedToken(state.Push(new GDMultiLineSplitToken()));
            state.PassLeftSlashChar();
        }

        private GDStatement CompleteAsStatement(GDReadingState state, string sequence)
        {
            _sequenceBuilder.Clear();
            _statementResolved = true;
            GDStatement statement = null;

            switch (sequence)
            {
                case "and":
                case "setget":
                case "else":
                    {
                        _statementResolved = false;
                        _resolvedStatement = null;
                        SendIntendationTokensToOwner();
                        Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x.IsSpace() || x.IsNewLine() || x.IsExpressionStopChar())));
                        state.PassString(sequence);
                        return null;
                    }
                case "if":
                    {
                        var s = new GDIfStatement(CurrentResolvedIntendationInSpaces);
                        s.IfBranch.Add(new GDIfKeyword());
                        statement = s;
                        break;
                    }
                case "for":
                    {
                        var s = new GDForStatement(CurrentResolvedIntendationInSpaces);
                        s.Add(new GDForKeyword());
                        statement = s;
                        break;
                    }
                case "while":
                    {
                        var s = new GDWhileStatement(CurrentResolvedIntendationInSpaces);
                        s.Add(new GDWhileKeyword());
                        statement = s;
                        break;
                    }
                case "match":
                    {
                        var s = new GDMatchStatement(CurrentResolvedIntendationInSpaces);
                        s.Add(new GDMatchKeyword());
                        statement = s;
                        break;
                    }
                case "var":
                    {
                        var s = new GDVariableDeclarationStatement(CurrentResolvedIntendationInSpaces);
                        s.Add(new GDVarKeyword());
                        statement = s;
                        break;
                    }
                default:
                    {
                        _resolvedAsExpression = true;
                        return _resolvedStatement = CompleteAsExpressionStatement(state, sequence);
                    }
            }

            SendIntendationTokensToOwner();
            Owner.HandleReceivedToken(statement);
            state.Push(statement);

            _resolvedStatement = statement;
            return statement;
        }

        private GDExpressionStatement CompleteAsExpressionStatement(GDReadingState state)
        {
            var statement = new GDExpressionStatement(CurrentResolvedIntendationInSpaces);

            SendIntendationTokensToOwner();
            Owner.HandleReceivedToken(statement);
            state.Push(statement);

            return statement;
        }

        private GDExpressionStatement CompleteAsExpressionStatement(GDReadingState state, string sequence)
        {
            var statement = CompleteAsExpressionStatement(state);
            state.PassString(sequence);
            return statement;
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_sequenceBuilder.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                _sequenceBuilder.Clear();

                CompleteAsStatement(state, sequence);
                ResetIntendation();
                return;
            }

            base.ForceComplete(state);

            if (!_statementResolved)
                PassIntendationSequence(state);
        }
    }
}