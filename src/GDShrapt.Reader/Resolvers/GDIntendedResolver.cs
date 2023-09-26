using System.Text;

namespace GDShrapt.Reader
{
    internal abstract class GDIntendedResolver : GDResolver
    {
        public int LineIntendationThreshold { get; }

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        int _lineIntendation;
        bool _firstLine = true;

        public int CalculatedIntendation => _lineIntendation;

        bool _lineIntendationEnded;
        int _spaceCounter;
        bool _inComment;
        bool _intendationTokensSent;

        new IIntendedTokenReceiver Owner { get; }

        public bool AllowZeroIntendationOnFirstLine { get; set; }

        public GDIntendedResolver(IIntendedTokenReceiver owner, int lineIntendation)
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
        internal abstract void HandleSharpCharAfterIntendation(GDReadingState state);

        bool HandleIntendation(char c, GDReadingState state)
        {
            if (AllowZeroIntendationOnFirstLine && _firstLine && !c.IsSpace())
            {
                _lineIntendationEnded = true;
                return false;
            }

            // Every child must start with line intendation equals intentation of parent plus 1
            if (!_lineIntendationEnded)
            {
                if (c == '\n')
                {
                    _firstLine = false;
                    _inComment = false;
                    _spaceCounter = 0;
                    _lineIntendation = 0;
                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (_inComment)
                {
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

                if (c == ' ' && state.Settings.ReadFourSpacesAsIntendation)
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

                    // The 'end of the block' condition
                    if (LineIntendationThreshold > _lineIntendation)
                    {
                        OnIntendationThresholdMet(state);
                        state.Pop();

                        // Pass all data to the previous node
                        for (int i = 0; i < _sequenceBuilder.Length; i++)
                            state.PassChar(_sequenceBuilder[i]);

                        state.PassChar(c);
                        return true;
                    }

                    if (LineIntendationThreshold < _lineIntendation)
                    {
                        // TODO: warning invalid extra intendation
                    }
                }
            }

            // It's OK
            return false;
        }

        protected virtual void OnIntendationThresholdMet(GDReadingState state)
        {
            // Nothing
        }

        internal sealed override void HandleNewLineChar(GDReadingState state)
        {
            if (HandleIntendation('\n', state))
                return;

            HandleNewLineAfterIntendation(state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (_lineIntendationEnded)
                HandleSharpCharAfterIntendation(state);
            else
            {
                _inComment = true;
                HandleIntendation('#', state);
            }
        }

        protected void SendIntendationTokensToOwner()
        {
            if (_intendationTokensSent)
                return;

            _intendationTokensSent = true;

            GDComment comment = null;
            GDSpace space = null;

            for (int i = 0; i < _sequenceBuilder.Length; i++)
            {
                var c = _sequenceBuilder[i];
                switch (c)
                {
                    case '\t':
                    case ' ':
                        if (comment != null)
                            comment.Append(c);
                        else
                        {
                            if (space == null)
                                space = new GDSpace();
                            space.Append(c);
                        }
                        break;
                    case '#':
                        if (space != null)
                        {
                            space.Complete();
                            Owner.HandleReceivedToken(space);
                            space = null;
                        }

                        if (comment == null)
                            comment = new GDComment();
                        comment.Append(c);
                        break;
                    case '\n':
                        if (space != null)
                        {
                            space.Complete();
                            Owner.HandleReceivedToken(space);
                            space = null;
                        }

                        if (comment != null)
                        {
                            comment.Complete();
                            Owner.HandleReceivedToken(comment);
                            comment = null;
                        }

                        Owner.HandleReceivedToken(new GDNewLine());
                        break;
                    default:
                        comment.Append(c);
                        break;
                }
            }

            if (space != null)
            {
                space.Complete();
                Owner.HandleReceivedToken(new GDIntendation()
                {
                    Sequence = space.Sequence,
                    LineIntendationThreshold = LineIntendationThreshold
                });
            }
            else
            {
                Owner.HandleReceivedToken(new GDIntendation()
                {
                    Sequence = string.Empty,
                    LineIntendationThreshold = LineIntendationThreshold
                });
            }
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
            _intendationTokensSent = false;
            _firstLine = true;
        }
    }
}