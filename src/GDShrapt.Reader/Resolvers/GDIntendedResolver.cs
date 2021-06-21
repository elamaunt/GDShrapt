using System.Text;

namespace GDShrapt.Reader
{
    internal abstract class GDIntendedResolver : GDResolver
    {
        public int LineIntendationThreshold { get; }

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        int _lineIntendation;
        bool _lineIntendationEnded;
        int _spaceCounter;

        new IIntendationReceiver Owner { get; }

        public GDIntendedResolver(IIntendationReceiver owner, int lineIntendation)
            : base(owner)
        {
            Owner = owner;
            LineIntendationThreshold = lineIntendation;
        }

        internal sealed override void HandleChar(char c, GDReadingState state)
        {
            if (HandleIntendation(c, state))
                return;

            HandleCharAfterIntendation(c, state);
        }

        internal abstract void HandleCharAfterIntendation(char c, GDReadingState state);
        internal abstract void HandleNewLineAfterIntendation(GDReadingState state);

        bool HandleIntendation(char c, GDReadingState state)
        {
            // Every child must start with line intendation equals intentation of parent plus 1
            if (!_lineIntendationEnded)
            {
                if (c == '\n')
                {
                    _spaceCounter = 0;
                    _lineIntendation = 0;
                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (c == '\t')
                {
                    if (_spaceCounter > 0)
                    {
                        // TODO: warning spaces before tabs
                    }

                    _spaceCounter = 0;
                    _lineIntendation++;
                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (c == ' ' && state.Settings.ConvertFourSpacesIntoTabs)
                {
                    _spaceCounter++;
                    _sequenceBuilder.Append(c);

                    if (_spaceCounter == 4)
                    {
                        _spaceCounter = 0;
                        _lineIntendation++;
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
                        for (int i = 0; i < _sequenceBuilder.Length; i++)
                            state.PassChar(_sequenceBuilder[i]);

                        state.PassChar(c);
                        return true;
                    }
                }

            }

            return false;
        }

        internal sealed override void HandleNewLineChar(GDReadingState state)
        {
            if (HandleIntendation('\n', state))
                return;

            HandleNewLineAfterIntendation(state);
        }

        protected void SendIntendationToOwner()
        {
            Owner.HandleReceivedToken(new GDIntendation()
            {
                Sequence = _sequenceBuilder.ToString(),
                LineIntendationThreshold = LineIntendationThreshold
            });
        }

        protected void PassIntendationSequence(GDReadingState state)
        {
            for (int i = 0; i < _sequenceBuilder.Length; i++)
                state.PassChar(_sequenceBuilder[i]);
        }

        protected void ResetIntendation()
        {
            _sequenceBuilder.Clear();
            _lineIntendation = 0;
            _lineIntendationEnded = false;
            _spaceCounter = 0;
        }
    }
}