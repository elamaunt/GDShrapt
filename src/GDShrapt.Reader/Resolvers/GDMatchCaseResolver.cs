using System;

namespace GDShrapt.Reader
{
    internal class GDMatchCaseResolver : GDNode
    {
        readonly Action<GDMatchCaseDeclaration> _handler;

        readonly int _lineIntendationThreshold;
        int _lineIntendation;
        bool _lineIntendationEnded;

        int _spaceCounter;

        public GDMatchCaseResolver(int lineIntendation, Action<GDMatchCaseDeclaration> handler)
        {
            _lineIntendationThreshold = lineIntendation;
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Every match case must start with line intendation equals intentation of parent plus 1
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

            var declaration = new GDMatchCaseDeclaration(_lineIntendationThreshold);
            _handler(declaration);
            state.PushNode(declaration);
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            _lineIntendation = 0;
            _lineIntendationEnded = false;
            _spaceCounter = 0;
        }
    }
}
