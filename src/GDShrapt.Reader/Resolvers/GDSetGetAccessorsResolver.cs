using System.Text;

namespace GDShrapt.Reader
{
    internal class GDSetGetAccessorsResolver<T> : GDIntendedResolver
        where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
    {
        public new T Owner { get; }
        public ITokenOrSkipReceiver<GDComma> CommaReceiver { get; }

        readonly StringBuilder _sequenceBuilder = new StringBuilder(8);

        State _state = State.Initial;
        bool _ignoreNewLine;
        bool _checkComma;

        private enum State
        {
            Initial,
            GotSetKeyword,
            GotGetKeyword,
        }

        public GDSetGetAccessorsResolver(T owner, ITokenOrSkipReceiver<GDComma> commaReceiver, bool allowZeroIntendationOnFirstLine, bool checkCommaAtStart, int lineIntendation)
                : base(owner, lineIntendation)
        {
            AllowZeroIntendationOnFirstLine = allowZeroIntendationOnFirstLine;
            Owner = owner;
            CommaReceiver = commaReceiver;
            _checkComma = checkCommaAtStart;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            switch (_sequenceBuilder.Length)
            {
                case 0:
                    if (_checkComma)
                    {
                        _checkComma = false;

                        if (c == ',')
                        {
                            SendIntendationTokensToOwner();
                            ResetIntendation();
                            CommaReceiver.HandleReceivedToken(new GDComma());
                            return;
                        }
                        else
                        {
                            CommaReceiver.HandleReceivedTokenSkip();
                        }
                    }

                    if (c.IsSpace())
                    {
                        Owner.HandleReceivedToken(state.PushAndPass(new GDSpace(), c));
                    }
                    else if (c == 's')
                    {
                        _sequenceBuilder.Append(c);
                        _state = State.GotSetKeyword;
                    }
                    else if (c == 'g')
                    {
                        _sequenceBuilder.Append(c);
                        _state = State.GotGetKeyword;
                    }
                    else
                    {
                        Owner.HandleReceivedTokenSkip();
                        state.Pop();
                        PassIntendationSequence(state);
                        state.PassChar(c);
                    }
                    break;
                case 1:
                    if (c == 'e')
                    {
                        _sequenceBuilder.Append(c);
                    }
                    else
                    {
                        Owner.HandleReceivedTokenSkip();

                        state.Pop();
                        PassIntendationSequence(state);
                        state.PassChar(_sequenceBuilder[0]);
                        state.PassChar(c);
                    }
                    break;
                case 2:
                    if (c == 't')
                    {
                        _sequenceBuilder.Append(c);
                    }
                    else
                    {
                        Owner.HandleReceivedTokenSkip();
                        state.Pop();
                        PassIntendationSequence(state);
                        state.PassChar(_sequenceBuilder[0]);
                        state.PassChar(_sequenceBuilder[1]);
                        state.PassChar(c);
                    }
                    break;
                default:
                    if (c.IsSpace())
                    {
                        _sequenceBuilder.Append(c);
                        return;
                    }

                    if (_state == State.GotGetKeyword && c == ':')
                    {
                        SendIntendationTokensToOwner();
                        state.Pop();
                        var accessor = new GDGetAccessorBodyDeclaration(CurrentResolvedIntendationInSpaces);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        PassStoredSequence(state);
                        state.PassChar(c);
                        return;
                    }

                    if (_state == State.GotSetKeyword && c == '(')
                    {
                        SendIntendationTokensToOwner();
                        state.Pop();
                        var accessor = new GDSetAccessorBodyDeclaration(CurrentResolvedIntendationInSpaces);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        PassStoredSequence(state);
                        state.PassChar(c);
                        return;
                    }

                    if (c == '=')
                    {
                        SendIntendationTokensToOwner();
                        state.Pop();

                        GDReader reader;
                        if (_state == State.GotGetKeyword)
                        {
                            var accessor = new GDGetAccessorMethodDeclaration(CurrentResolvedIntendationInSpaces);
                            Owner.HandleReceivedToken(accessor);
                            reader = accessor;
                        }
                        else
                        {
                            var accessor = new GDSetAccessorMethodDeclaration(CurrentResolvedIntendationInSpaces);
                            Owner.HandleReceivedToken(accessor);
                            reader = accessor;
                        }

                        state.Push(reader);
                        PassStoredSequence(state);
                        state.PassChar(c);

                        return;
                    }

                    Owner.HandleReceivedTokenSkip();
                    state.Pop();
                    PassIntendationSequence(state);
                    PassStoredSequence(state);
                    state.PassChar(c);
                    return;
            }
        }

        protected override void OnIntendationThresholdMet(GDReadingState state)
        {
            if (_checkComma)
            {
                _checkComma = false;
                CommaReceiver.HandleReceivedTokenSkip();
            }

            Owner.HandleReceivedTokenSkip();
            base.OnIntendationThresholdMet(state);
        }

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            if (_ignoreNewLine)
            {
                _sequenceBuilder.Append('\n');
                _ignoreNewLine = false;
                return;
            }

            if (_checkComma)
            {
                _checkComma = false;
                CommaReceiver.HandleReceivedTokenSkip();
            }

            Owner.HandleReceivedTokenSkip();
            state.Pop();
            PassIntendationSequence(state);
            PassStoredSequence(state);
            state.PassNewLine();
        }

        internal override void HandleCarriageReturnCharAfterIntendation(GDReadingState state)
        {
            // CR handling mirrors NL - no accessors found, pop and pass through
            if (_checkComma)
            {
                _checkComma = false;
                CommaReceiver.HandleReceivedTokenSkip();
            }

            Owner.HandleReceivedTokenSkip();
            state.Pop();
            PassIntendationSequence(state);
            PassStoredSequence(state);
            state.PassCarriageReturnChar();
        }

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
            state.Pop();
            PassIntendationSequence(state);
            PassStoredSequence(state);
            state.PassSharpChar();
        }

        internal override void HandleLeftSlashCharAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder.Length < 3)
            {
                Owner.HandleReceivedTokenSkip();
                state.Pop();
                PassIntendationSequence(state);
                PassStoredSequence(state);
                state.PassLeftSlashChar();
                return;
            }

            _sequenceBuilder.Append('\\');
            _ignoreNewLine = true;
        }

        private void PassStoredSequence(GDReadingState state)
        {
            for (int i = 0; i < _sequenceBuilder.Length; i++)
                state.PassChar(_sequenceBuilder[i]);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            base.ForceComplete(state);
            Owner.HandleReceivedTokenSkip();
            PassIntendationSequence(state);
            PassStoredSequence(state);
        }
    }
}
