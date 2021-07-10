namespace GDShrapt.Reader
{
    public sealed class GDMatchStatement : GDStatement,
        ITokenOrSkipReceiver<GDMatchKeyword>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDExpression>
    {
        public GDMatchKeyword MatchKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDExpression Value
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDColon Colon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDMatchCasesList Cases 
        {
            get => _form.Token3 ?? (_form.Token3 = new GDMatchCasesList(LineIntendation + 1));
            set => _form.Token3 = value;
        }

        enum State
        {
            Match,
            Value,
            Colon,
            Cases,
            Completed
        }

        readonly GDTokensForm<State, GDMatchKeyword, GDExpression, GDColon, GDMatchCasesList> _form;
        public override GDTokensForm Form => _form;

        internal GDMatchStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDMatchKeyword, GDExpression, GDColon, GDMatchCasesList>(this);
        }

        public GDMatchStatement()
        {
            _form = new GDTokensForm<State, GDMatchKeyword, GDExpression, GDColon, GDMatchCasesList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Match:
                    state.PushAndPass(new GDKeywordResolver<GDMatchKeyword>(this), c);
                    break;
                case State.Value:
                    state.PushAndPass(new GDExpressionResolver(this), c);
                    break;
                case State.Colon:
                    state.PushAndPass(new GDSingleCharTokenResolver<GDColon>(this), c);
                    break;
                case State.Cases:
                    _form.State = State.Completed;
                    state.PushAndPass(Cases, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Value:
                case State.Colon:
                case State.Cases:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Cases);
                    break;
                default:
                    state.PopAndPassNewLine(); 
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDMatchStatement();
        }

        void ITokenReceiver<GDMatchKeyword>.HandleReceivedToken(GDMatchKeyword token)
        {
            if (_form.State == State.Match)
            {
                MatchKeyword = token;
                _form.State = State.Value;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDMatchKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Match)
            {
                _form.State = State.Value;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                Colon = token;
                _form.State = State.Cases;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Cases;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Value)
            {
                Value = token;
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Value)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}