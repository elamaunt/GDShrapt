using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStatementResolver : GDIntendedResolver
    {
        // TODO: make if statement self sufficient
        GDIfStatement _ifStatement;
        bool _statementResolved;

        readonly StringBuilder _sequenceBuilder = new StringBuilder();


        new IStatementsReceiver Owner { get; }

        public GDStatementResolver(IStatementsReceiver owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (!_statementResolved)
            {
                if (_sequenceBuilder.Length == 0)
                {
                    if (IsSpace(c))
                    {
                        Owner.HandleReceivedToken(state.Push(new GDSpace()));
                        state.PassChar(c);
                        return;
                    }
                }

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
            else
            {
                if (IsSpace(c))
                {
                    Owner.HandleReceivedToken(state.Push(new GDSpace()));
                    state.PassChar(c);
                    return;
                }

                Owner.HandleReceivedToken(state.Push(new GDInvalidToken(' ', '\n')));
                state.PassChar(c);
            }
            

            


            // Old code
                /*if (char.IsLetter(c) && _sequenceBuilder.Length < 6)
                {
                    _sequenceBuilder.Append(c);
                }
                else
                {
                    if (_sequenceBuilder.Length == 0 && IsSpace(c))
                        return;

                    var sequence = _sequenceBuilder.ToString();

                    ResetSequence();

                    if (IsSpace(c) || c == ':' || c == ';')
                    {
                        CompleteAsStatement(state, sequence);
                    }
                    else
                    {
                        CompleteAsExpressionStatement(state, sequence);
                        state.PassChar(c);
                    }
                }*/
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_statementResolved)
            {
                _statementResolved = false;
                _sequenceBuilder.Clear();
            }
            else
            {
                if (_sequenceBuilder?.Length > 0)
                {
                    var sequence = _sequenceBuilder.ToString();
                    _sequenceBuilder.Clear();
                    CompleteAsStatement(state, sequence);
                    state.PassNewLine();
                    return;
                }
            }

            Owner.HandleReceivedToken(new GDNewLine());
            ResetIntendation();
        }

        private GDStatement CompleteAsStatement(GDReadingState state, string sequence)
        {
            _sequenceBuilder.Clear();
            _statementResolved = true;

            GDStatement statement = null;

            switch (sequence)
            {
                // TODO: move logic to if statement
                /*case "else":
                    if (_ifStatement != null)
                    {
                        _ifStatement.HandleFalseStatements(state);
                        _ifStatement = null;
                    }

                    return;
                case "elif":
                    if (_ifStatement != null)
                    {
                        var newIfStatement = new GDIfStatement(LineIntendationThreshold);
                        statement = newIfStatement;
                        _ifStatement.FalseStatements.Add(statement);
                        _ifStatement = newIfStatement;
                        state.Push(statement);
                    }

                    return;*/
                case "if":
                    statement = _ifStatement = new GDIfStatement(LineIntendationThreshold);
                    break;
                case "for":
                    _ifStatement = null;
                    statement = new GDForStatement(LineIntendationThreshold);
                    break;
                case "while":
                    _ifStatement = null;
                    statement = new GDWhileStatement(LineIntendationThreshold);
                    break;
                case "match":
                    _ifStatement = null;
                    statement = new GDMatchStatement(LineIntendationThreshold);
                    break;
                case "var":
                    _ifStatement = null;
                    statement = new GDVariableDeclarationStatement(LineIntendationThreshold);
                    break;
                default:
                    {
                        return CompleteAsExpressionStatement(state, sequence);
                    }
            }

            Owner.HandleReceivedToken(statement);

            state.Push(statement);

            for (int i = 0; i < sequence.Length; i++)
                state.PassChar(sequence[i]);

            return statement;
        }

        private GDExpressionStatement CompleteAsExpressionStatement(GDReadingState state, string sequence)
        {
            _ifStatement = null;
            var statement = new GDExpressionStatement();

            Owner.HandleReceivedToken(statement);
            state.Push(statement);

            for (int i = 0; i < sequence.Length; i++)
                state.PassChar(sequence[i]);

            return statement;
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (!_statementResolved)
            {
                var sequence = _sequenceBuilder.ToString();
                _sequenceBuilder.Clear();
                CompleteAsStatement(state, sequence)?.ForceComplete(state);
            }

            base.ForceComplete(state);
        }
    }
}