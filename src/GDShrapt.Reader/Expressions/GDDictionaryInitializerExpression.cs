namespace GDShrapt.Reader
{
    public sealed class GDDictionaryInitializerExpression : GDExpression,
        ITokenOrSkipReceiver<GDFigureOpenBracket>,
        ITokenOrSkipReceiver<GDFigureCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.DictionaryInitializer);

        public GDFigureOpenBracket FigureOpenBracket
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDDictionaryKeyValueDeclarationList KeyValues
        {
            get => _form.Token1 ?? (_form.Token1 = new GDDictionaryKeyValueDeclarationList());
            set => _form.Token1 = value;
        }
        public GDFigureCloseBracket FigureCloseBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            FigureOpenBracket,
            KeyValues,
            FigureCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDFigureOpenBracket, GDDictionaryKeyValueDeclarationList, GDFigureCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDDictionaryInitializerExpression()
        {
            _form = new GDTokensForm<State, GDFigureOpenBracket, GDDictionaryKeyValueDeclarationList, GDFigureCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.FigureOpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveFigureOpenBracket(c, state);
                    break;
                case State.KeyValues:
                    _form.State = State.FigureCloseBracket;
                    state.PushAndPass(KeyValues, c);
                    break;
                case State.FigureCloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveFigureCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.State == State.KeyValues)
            {
                _form.State = State.FigureCloseBracket;
                state.PushAndPassNewLine(KeyValues);
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDDictionaryInitializerExpression();
        }

        void ITokenReceiver<GDFigureOpenBracket>.HandleReceivedToken(GDFigureOpenBracket token)
        {
            if (_form.State == State.FigureOpenBracket)
            {
                _form.State = State.KeyValues;
                FigureOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDFigureOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.FigureOpenBracket)
            {
                _form.State = State.KeyValues;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDFigureCloseBracket>.HandleReceivedToken(GDFigureCloseBracket token)
        {
            if (_form.State == State.FigureCloseBracket)
            {
                _form.State = State.Completed;
                FigureCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDFigureCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.FigureCloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
