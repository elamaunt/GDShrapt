using System;
using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStatementResolver : GDNode
    {
        readonly Action<GDStatement> _handler;
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        int _lineIntendationThreshold;
        int _lineIntendation;
        bool _lineIntendationEnded;

        int _spaceCounter;

        GDIfStatement _ifStatement;

        public GDStatementResolver(int lineIntendation, Action<GDStatement> handler)
        {
            _lineIntendationThreshold = lineIntendation;
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Every statement must start with line intendation equals intentation of parent plus 1
            if (!_lineIntendationEnded)
            {
                if (c == '\t')
                {
                    _spaceCounter = 0;
                    _lineIntendation++;
                    return;
                }
                else
                {
                    if (c == ' ' && state.Settings.ConvertFourSpacesIntoTabs)
                    {
                        _spaceCounter++;

                        if (_spaceCounter == 4)
                        {
                            _spaceCounter = 0;
                            HandleChar('\t', state);
                        }

                        return;
                    }
                    else
                    {
                        _lineIntendationEnded = true;

                        if (_lineIntendationThreshold != _lineIntendation)
                        {
                            state.PopNode();

                            // Pass all data to the previous node
                            state.PassLineFinish();

                            for (int i = 0; i < _lineIntendation; i++)
                                state.PassChar('\t');
                            for (int i = 0; i < _spaceCounter; i++)
                                state.PassChar(' ');

                            state.PassChar(c);
                            return;
                        }
                    }
                }
            }

            if (char.IsLetter(c) && _sequenceBuilder.Length < 6)
            {
                _sequenceBuilder.Append(c);
            }
            else
            {
                var sequence = _sequenceBuilder.ToString();

                ResetSequence();

                if (IsSpace(c) || c == ':')
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
                _lineIntendation = 0;
                _lineIntendationEnded = false;
                _spaceCounter = 0;
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
                        var newIfStatement = new GDIfStatement(_lineIntendation);
                        statement = newIfStatement;
                        _ifStatement.FalseStatements.Add(statement);
                        _ifStatement = newIfStatement;
                        state.PushNode(statement);
                    }

                    return;
                case "if":
                    statement = _ifStatement = new GDIfStatement(_lineIntendation);
                    break;
                case "for":
                    statement = new GDForStatement(_lineIntendation);
                    break;
                case "while":
                    statement = new GDWhileStatement(_lineIntendation);
                    break;
                case "match":
                    statement = new GDMatchStatement(_lineIntendation);
                    break;
                case "yield":
                    statement = new GDYieldStatement(_lineIntendation);
                    break;
                case "var":
                    statement = new GDVariableDeclarationStatement(_lineIntendation);
                    break;
                case "return":
                    statement = new GDReturnStatement(_lineIntendation);
                    break;
                case "pass":
                    statement = new GDPassStatement(_lineIntendation);
                    break;
                default:
                    {
                        CompleteAsExpressionStatement(state, sequence);
                        return;
                    }
            }

            ReturnExpression(statement);
            state.PushNode(statement);
        }

        private void CompleteAsExpressionStatement(GDReadingState state, string sequence)
        {
            var statement = new GDExpressionStatement();

            ReturnExpression(statement);
            state.PushNode(statement);

            for (int i = 0; i < sequence.Length; i++)
                state.PassChar(sequence[i]);
        }

        private void ReturnExpression(GDStatement statement)
        {
            statement.EndLineComment = EndLineComment;
            EndLineComment = null;
            _handler(statement);
        }
    }
}