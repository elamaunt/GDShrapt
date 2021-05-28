using System;

namespace GDShrapt.Reader
{
    public class GDStatementResolver : GDCharSequence
    {
        readonly Action<GDStatement> _handler;

        int _lineIntendationThreshold;

        int _lineIntendation;
        bool _lineIntendationEnded;

        public GDStatementResolver(int lineIntendation, Action<GDStatement> handler)
        {
            _lineIntendationThreshold = lineIntendation;
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_lineIntendationEnded)
            {
                if (c == '\t')
                {
                    _lineIntendation++;
                    return;
                }
                else
                {
                    _lineIntendationEnded = true;

                    if (_lineIntendationThreshold != _lineIntendation)
                    {
                        state.PopNode();
                        // Pass intendation in next node
                        for (int i = 0; i < _lineIntendation; i++)
                            state.HandleChar('\t');
                        return;
                    }
                }
            }

            if (SequenceBuilder?.Length == 0 && (char.IsDigit(c) || c == '.' || c == '(' ))
            {
                var statement = new GDExpressionStatement();
                _handler(statement);
                state.PushNode(statement);
                state.HandleChar(c);
            }
            else
                base.HandleChar(c, state);
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (SequenceBuilder?.Length > 0)
            {
                CompleteSequence(state);
                state.FinishLine();
            }
            else
            {
                _lineIntendation = 0;
                _lineIntendationEnded = false;
                ResetSequence();
            }
        }

        internal override void CompleteSequence(GDReadingState state)
        {
            GDStatement statement = null;

            Sequence = SequenceBuilder.ToString();

            switch (Sequence)
            {
                case "if":
                    statement = new GDIfStatement(_lineIntendation);
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
                        ResetSequence();
                        return;
                    }
            }

            _handler(statement);
            state.PushNode(statement);
            ResetSequence();
        }
    }
}