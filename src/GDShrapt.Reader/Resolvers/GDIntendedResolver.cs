namespace GDShrapt.Reader
{
    internal abstract class GDIntendedResolver : GDResolver
    {
        public int LineIntendationThreshold { get; }
        int _lineIntendation;
        bool _lineIntendationEnded;
        int _spaceCounter;

        public GDIntendedResolver(IStyleTokensReceiver owner, int lineIntendation)
            : base(owner)
        {
            LineIntendationThreshold = lineIntendation;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (HandleIntendation(c, state))
                return;

            HandleCharAfterIntendation(c, state);
        }

        internal abstract void HandleCharAfterIntendation(char c, GDReadingState state);

        bool HandleIntendation(char c, GDReadingState state)
        {
            // Every child must start with line intendation equals intentation of parent plus 1
            if (!_lineIntendationEnded)
            {
                if (c == '\t')
                {
                    _spaceCounter = 0;
                    _lineIntendation++;
                    return true;
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

                        return true;
                    }
                    else
                    {
                        _lineIntendationEnded = true;

                        if (LineIntendationThreshold != _lineIntendation)
                        {
                            state.Pop();

                            // Pass all data to the previous node
                            state.PassLineFinish();

                            for (int i = 0; i < _lineIntendation; i++)
                                state.PassChar('\t');
                            for (int i = 0; i < _spaceCounter; i++)
                                state.PassChar(' ');

                            state.PassChar(c);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            ResetIntendation();
        }

        protected void ResetIntendation()
        {
            _lineIntendation = 0;
            _lineIntendationEnded = false;
            _spaceCounter = 0;
        }
    }
}