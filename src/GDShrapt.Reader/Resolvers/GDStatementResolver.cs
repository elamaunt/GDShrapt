using System;
using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStatementResolver : GDIntendedResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        GDIfStatement _ifStatement;

        public GDStatementResolver(ITokensContainer owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (char.IsLetter(c) && _sequenceBuilder.Length < 6)
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
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                CompleteAsStatement(state, sequence);
                state.PassLineFinish();
            }
            else
            {
                ResetIntendation();
                ResetSequence();
            }
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void CompleteAsStatement(GDReadingState state, string sequence)
        {
            GDStatement statement = null;

            switch (sequence)
            {
                case "else":
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

                    return;
                case "if":
                    statement = _ifStatement = new GDIfStatement(LineIntendationThreshold);
                    break;
                case "for":
                    statement = new GDForStatement(LineIntendationThreshold);
                    break;
                case "while":
                    statement = new GDWhileStatement(LineIntendationThreshold);
                    break;
                case "match":
                    statement = new GDMatchStatement(LineIntendationThreshold);
                    break;
                case "yield":
                    statement = new GDYieldStatement(LineIntendationThreshold);
                    break;
                case "var":
                    statement = new GDVariableDeclarationStatement(LineIntendationThreshold);
                    break;
                case "return":
                    statement = new GDReturnStatement(LineIntendationThreshold);
                    break;
                case "pass":
                    statement = new GDPassStatement(LineIntendationThreshold);
                    break;
                case "continue":
                    statement = new GDContinueStatement(LineIntendationThreshold);
                    break;
                case "break":
                    statement = new GDBreakStatement(LineIntendationThreshold);
                    break;
                case "breakpoint":
                    statement = new GDBreakPointStatement(LineIntendationThreshold);
                    break;
                default:
                    {
                        CompleteAsExpressionStatement(state, sequence);
                        return;
                    }
            }

            Append(statement);
            state.Push(statement);
        }

        private void CompleteAsExpressionStatement(GDReadingState state, string sequence)
        {
            var statement = new GDExpressionStatement();

            Append(statement);
            state.Push(statement);

            for (int i = 0; i < sequence.Length; i++)
                state.PassChar(sequence[i]);
        }
    }
}