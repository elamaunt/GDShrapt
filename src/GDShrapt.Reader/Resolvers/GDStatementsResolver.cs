using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStatementsResolver : GDIntendedResolver
    {
        bool _statementResolved;

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IStatementsReceiver Owner { get; }

        public GDStatementsResolver(IStatementsReceiver owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
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

                Owner.ResolveInvalidToken(c, state, x => !x.IsSpace());
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

                if (char.IsLetter(c))
                {
                    _sequenceBuilder.Append(c);
                }
                else
                {
                    CompleteAsExpressionStatement(state);
                    state.PassChar(c);
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

            _statementResolved = false;
            ResetIntendation();
            state.PassNewLine();
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

        private GDStatement CompleteAsStatement(GDReadingState state, string sequence)
        {
            _sequenceBuilder.Clear();
            _statementResolved = true;
            GDStatement statement = null;

            switch (sequence)
            {
                case "if":
                    {
                        var s = new GDIfStatement(LineIntendationThreshold);
                        s.SendKeyword(new GDIfKeyword());
                        statement = s;
                        break;
                    }
                case "for":
                    {
                        var s = new GDForStatement(LineIntendationThreshold);
                        s.SendKeyword(new GDForKeyword());
                        statement = s;
                        break;
                    }
                case "while":
                    {
                        var s = new GDWhileStatement(LineIntendationThreshold);
                        s.SendKeyword(new GDWhileKeyword());
                        statement = s;
                        break;
                    }
                case "match":
                    {
                        var s = new GDMatchStatement(LineIntendationThreshold);
                        s.SendKeyword(new GDMatchKeyword());
                        statement = s;
                        break;
                    }
                case "var":
                    {
                        var s = new GDVariableDeclarationStatement(LineIntendationThreshold);
                        s.SendKeyword(new GDVarKeyword());
                        statement = s;
                        break;
                    }
                default:
                    {
                        return CompleteAsExpressionStatement(state, sequence);
                    }
            }

            SendIntendationTokensToOwner();
            Owner.HandleReceivedToken(statement);
            state.Push(statement);
            return statement;
        }

        private GDExpressionStatement CompleteAsExpressionStatement(GDReadingState state)
        {
            var statement = new GDExpressionStatement();

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