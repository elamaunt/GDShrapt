using System.Text;

namespace GDShrapt.Reader
{
    internal abstract class GDIntendedResolver : GDResolver
    {
        public int CurrentResolvedIntendationInSpaces { get; private set; }
        public int LineIntendationInSpacesThreshold { get; }

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        int _lineIntendation;
        bool _firstLine = true;

        public int CalculatedIntendation => _lineIntendation;

        bool _lineIntendationEnded;
        bool _inComment;
        bool _intendationTokensSent;
        bool _lineSplitted;

        new IIntendedTokenReceiver Owner { get; }

        public bool AllowZeroIntendationOnFirstLine { get; set; }

        public GDIntendedResolver(IIntendedTokenReceiver owner, int lineIntendationInSpaces)
            : base(owner)
        {
            Owner = owner;
            LineIntendationInSpacesThreshold = lineIntendationInSpaces;
        }

        internal sealed override void HandleChar(char c, GDReadingState state)
        {
            if (HandleIntendation(c, state))
                return;

            HandleCharAfterIntendation(c, state);
        }

        internal abstract void HandleCharAfterIntendation(char c, GDReadingState state);
        internal abstract void HandleNewLineAfterIntendation(GDReadingState state);
        internal abstract void HandleCarriageReturnCharAfterIntendation(GDReadingState state);
        internal abstract void HandleSharpCharAfterIntendation(GDReadingState state);
        internal abstract void HandleLeftSlashCharAfterIntendation(GDReadingState state);

        bool HandleIntendation(char c, GDReadingState state)
        {
            if (_lineIntendationEnded)
                return false;

            //int singleIntendationSpacesCount = 4;
            //bool singleIntendationResolved = state.IntendationInSpacesCount.HasValue;

            //if (singleIntendationResolved)
            //    singleIntendationSpacesCount = state.IntendationInSpacesCount.Value;

            if (AllowZeroIntendationOnFirstLine && _firstLine && !c.IsSpace() && !c.IsNewLine() && c != '#' && c != '\\')
            {
                _lineIntendationEnded = true;
                return false;
            }

            // Every child must start with line intendation equals intentation of parent plus 1
            if (!_lineIntendationEnded)
            {
                if (c == '\n')
                {
                    if (_lineSplitted)
                        _lineSplitted = false;
                    else
                    {
                        _firstLine = false;
                        _inComment = false;
                        _lineIntendation = 0;
                    }

                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (_inComment)
                {
                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (c == '\\')
                {
                    _lineSplitted = true;
                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (c == '\t')
                {
                    _lineIntendation += state.Settings.SingleTabSpacesCost;
                    _sequenceBuilder.Append(c);
                    return true;
                }

                if (c == ' ')
                {
                    _lineIntendation++;
                    _sequenceBuilder.Append(c);

                    return true;
                }
                else
                {
                    _lineIntendationEnded = true;

                    // The 'end of the block' condition
                    if (LineIntendationInSpacesThreshold > _lineIntendation)
                    {
                        OnIntendationThresholdMet(state);
                        state.Pop();

                        // Pass all data to the previous node
                        for (int i = 0; i < _sequenceBuilder.Length; i++)
                        {
                            state.AdvanceInputPosition();
                            state.PassChar(_sequenceBuilder[i]);
                        }

                        state.PassChar(c);
                        return true;
                    }

                    CurrentResolvedIntendationInSpaces = _lineIntendation;
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

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            if (_lineIntendationEnded)
                HandleLeftSlashCharAfterIntendation(state);
            else
            {
                HandleIntendation('\\', state);
            }
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (!_lineIntendationEnded)
            {
                // Buffer CR just like we buffer other whitespace characters
                _sequenceBuilder.Append('\r');
            }
            else
            {
                // After indentation ended, delegate to subclass handler (mirrors HandleNewLineChar pattern)
                HandleCarriageReturnCharAfterIntendation(state);
            }
        }

        protected void SendIntendationTokensToOwner()
        {
            if (_intendationTokensSent)
                return;

            _intendationTokensSent = true;

            GDComment comment = null;
            GDSpace space = null;
            GDMultiLineSplitToken split = null;

            for (int i = 0; i < _sequenceBuilder.Length; i++)
            {
                var c = _sequenceBuilder[i];
                switch (c)
                {
                    case '\t':
                    case ' ':
                        if (split != null)
                            split.Append(c);
                        else if (comment != null)
                            comment.Append(c);
                        else
                        {
                            if (space == null)
                                space = new GDSpace();
                            space.Append(c);
                        }
                        break;
                    case '#':
                        if (split != null)
                            split.Append(c);
                        else
                        {
                            if (space != null)
                            {
                                space.Complete();
                                Owner.HandleReceivedToken(space);
                                space = null;
                            }

                            if (comment == null)
                                comment = new GDComment();
                            comment.Append(c);
                        }
                        break;
                    case '\r':
                        // CR should be sent before the corresponding NL
                        if (split != null)
                            split.Append(c);
                        else if (comment != null)
                            comment.Append(c);
                        else
                        {
                            // Flush any pending space before CR
                            if (space != null)
                            {
                                space.Complete();
                                Owner.HandleReceivedToken(space);
                                space = null;
                            }
                            Owner.HandleReceivedToken(new GDCarriageReturnToken());
                        }
                        break;
                    case '\n':
                        if (split != null)
                        {
                            split.Append(c);
                            split.Complete();
                            Owner.HandleReceivedToken(split);
                            split = null;
                        }
                        else
                        {
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
                        }
                        break;
                    case '\\':
                        if (space != null)
                        {
                            space.Complete();
                            Owner.HandleReceivedToken(space);
                            space = null;
                        }

                        if (comment != null)
                        {
                            comment.Append(c);
                        }
                        else
                        {
                            if (split == null)
                                split = new GDMultiLineSplitToken();
                            split.Append(c);
                        }
                        break;
                    default:
                        if (split != null)
                            split.Append(c);
                        else
                            comment.Append(c);
                        break;
                }
            }

            if (split != null)
            {
                split.Complete();
                Owner.HandleReceivedToken(split);
                split = null;
            }

            if (comment != null)
            {
                comment.Complete();
                Owner.HandleReceivedToken(comment);
                comment = null;
            }

            if (space != null)
            {
                space.Complete();
                Owner.HandleReceivedToken(new GDIntendation()
                {
                    Sequence = space.Sequence,
                    LineIntendationThreshold = CurrentResolvedIntendationInSpaces
                });
            }
            else
            {
                if (_firstLine && AllowZeroIntendationOnFirstLine)
                    return;

                Owner.HandleReceivedToken(new GDIntendation()
                {
                    Sequence = string.Empty,
                    LineIntendationThreshold = CurrentResolvedIntendationInSpaces
                });
            }
        }

        protected void PassIntendationSequence(GDReadingState state)
        {
            if (_intendationTokensSent)
                return;

            for (int i = 0; i < _sequenceBuilder.Length; i++)
                state.PassChar(_sequenceBuilder[i]);

            ResetIntendation();
        }

        protected void ResetIntendation()
        {
            _sequenceBuilder.Clear();
            _lineIntendation = 0;
            _lineIntendationEnded = false;
            _intendationTokensSent = false;
            _firstLine = true;
        }
    }
}