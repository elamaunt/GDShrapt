namespace GDShrapt.Reader
{
    public sealed class GDEnumDeclaration : GDClassMember,
        IKeywordReceiver<GDEnumKeyword>,
        ITokenReceiver<GDFigureOpenBracket>,
        ITokenReceiver<GDFigureCloseBracket>
    {
        internal GDEnumKeyword EnumKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        internal GDFigureOpenBracket FigureOpenBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDEnumValuesList Values { get => _form.Token3 ?? (_form.Token3 = new GDEnumValuesList()); }
        internal GDFigureCloseBracket FigureCloseBracket
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        enum State
        {
            Enum,
            Identifier,
            FigureOpenBracket,
            Values,
            FigureCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDEnumKeyword, GDIdentifier, GDFigureOpenBracket, GDEnumValuesList, GDFigureCloseBracket> _form;
        internal override GDTokensForm Form => _form;
        internal GDEnumDeclaration(int intendation)
           : base(intendation)
        {
            _form = new GDTokensForm<State, GDEnumKeyword, GDIdentifier, GDFigureOpenBracket, GDEnumValuesList, GDFigureCloseBracket>(this);
        }
        public GDEnumDeclaration()
        {
            _form = new GDTokensForm<State, GDEnumKeyword, GDIdentifier, GDFigureOpenBracket, GDEnumValuesList, GDFigureCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) && _form.State != State.Values)
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Enum:
                    state.PushAndPass(new GDKeywordResolver<GDEnumKeyword>(this), c);
                    break;
                case State.Identifier:
                    if (c == '{')
                    {
                        _form.State = State.FigureOpenBracket;
                        goto case State.FigureOpenBracket;
                    }

                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.FigureOpenBracket;
                        state.Push(Identifier = new GDIdentifier());
                        state.PassChar(c);
                    }
                    else
                    {
                        _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsSpace(x) || x == '{' || IsIdentifierStartChar(x) || x == '\n')));
                        state.PassChar(c);
                    }
                    break;
                case State.FigureOpenBracket:
                    state.PushAndPass(new GDSingleCharTokenResolver<GDFigureOpenBracket>(this), c);
                    break;
                case State.Values:
                    _form.State = State.FigureCloseBracket;
                    state.Push(Values);
                    state.PassChar(c);
                    break;
                case State.FigureCloseBracket:
                    state.PushAndPass(new GDSingleCharTokenResolver<GDFigureCloseBracket>(this), c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.State == State.Values)
            {
                _form.State = State.FigureCloseBracket;
                state.PushAndPassNewLine(Values);
                return;
            }

            state.PopAndPassNewLine();
        }

        void IKeywordReceiver<GDEnumKeyword>.HandleReceivedToken(GDEnumKeyword token)
        {
            if (_form.State == State.Enum)
            {
                _form.State = State.Identifier;
                EnumKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDEnumKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Enum)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDFigureOpenBracket>.HandleReceivedToken(GDFigureOpenBracket token)
        {
            if (_form.State == State.FigureOpenBracket)
            {
                _form.State = State.Values;
                FigureOpenBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDFigureOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.FigureOpenBracket)
            {
                _form.State = State.Values;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDFigureCloseBracket>.HandleReceivedToken(GDFigureCloseBracket token)
        {
            if (_form.State == State.FigureCloseBracket)
            {
                _form.State = State.Completed;
                FigureCloseBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDFigureCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.FigureCloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
