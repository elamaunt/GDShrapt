namespace GDShrapt.Reader
{
    public sealed class GDDictionaryInitializerExpression : GDExpression,
        ITokenReceiver<GDFigureOpenBracket>,
        ITokenReceiver<GDFigureCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.DictionaryInitializer);

        internal GDFigureOpenBracket FigureOpenBracket
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDDictionaryKeyValueDeclarationList KeyValues { get => _form.Token1 ?? (_form.Token1 = new GDDictionaryKeyValueDeclarationList()); }
        internal GDFigureCloseBracket FigureCloseBracket
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
        internal override GDTokensForm Form => _form;
        public GDDictionaryInitializerExpression()
        {
            _form = new GDTokensForm<State, GDFigureOpenBracket, GDDictionaryKeyValueDeclarationList, GDFigureCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.FigureOpenBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveFigureOpenBracket(c, state);
                    break;
                case State.KeyValues:
                    _form.State = State.FigureCloseBracket;
                    state.PushAndPass(KeyValues, c);
                    break;
                case State.FigureCloseBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveFigureCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        void ITokenReceiver<GDFigureOpenBracket>.HandleReceivedToken(GDFigureOpenBracket token)
        {
            if (_form.State == State.FigureOpenBracket)
            {
                _form.State = State.KeyValues;
                FigureOpenBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDFigureOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.FigureOpenBracket)
            {
                _form.State = State.KeyValues;
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
