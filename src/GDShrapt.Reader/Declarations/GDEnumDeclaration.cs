namespace GDShrapt.Reader
{
    public sealed class GDEnumDeclaration : GDIdentifiableClassMember,
        ITokenOrSkipReceiver<GDEnumKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDFigureOpenBracket>,
        ITokenOrSkipReceiver<GDEnumValuesList>,
        ITokenOrSkipReceiver<GDFigureCloseBracket>
    {
        public GDEnumKeyword EnumKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public override GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDFigureOpenBracket FigureOpenBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDEnumValuesList Values 
        {
            get => _form.Token3 ?? (_form.Token3 = new GDEnumValuesList());
            set => _form.Token3 = value;
        }
        public GDFigureCloseBracket FigureCloseBracket
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public enum State
        {
            Enum,
            Identifier,
            FigureOpenBracket,
            Values,
            FigureCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDEnumKeyword, GDIdentifier, GDFigureOpenBracket, GDEnumValuesList, GDFigureCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public override bool IsStatic => true;
        public GDTokensForm<State, GDEnumKeyword, GDIdentifier, GDFigureOpenBracket, GDEnumValuesList, GDFigureCloseBracket> TypedForm => _form;

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
            if (_form.IsOrLowerState(State.Values))
            {
                _form.AddBeforeActiveToken(new GDNewLine());
                return;
            }

            state.PopAndPassNewLine();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Values))
            {
                _form.AddBeforeActiveToken(new GDCarriageReturnToken());
                return;
            }

            state.PopAndPassCarriageReturnChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDEnumDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDEnumKeyword>.HandleReceivedToken(GDEnumKeyword token)
        {
            if (_form.IsOrLowerState(State.Enum))
            {
                _form.State = State.Identifier;
                EnumKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDEnumKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Enum))
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDFigureOpenBracket>.HandleReceivedToken(GDFigureOpenBracket token)
        {
            if (_form.IsOrLowerState(State.FigureOpenBracket))
            {
                _form.State = State.Values;
                FigureOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDFigureOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.FigureOpenBracket))
            {
                _form.State = State.Values;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDFigureCloseBracket>.HandleReceivedToken(GDFigureCloseBracket token)
        {
            if (_form.IsOrLowerState(State.FigureCloseBracket))
            {
                _form.State = State.Completed;
                FigureCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDFigureCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.FigureCloseBracket))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                Identifier = token;
                _form.State = State.FigureOpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.FigureOpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDEnumValuesList>.HandleReceivedToken(GDEnumValuesList token)
        {
            if (_form.IsOrLowerState(State.Values))
            {
                Values = token;
                _form.State = State.FigureCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDEnumValuesList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Values))
            {
                _form.State = State.FigureCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
