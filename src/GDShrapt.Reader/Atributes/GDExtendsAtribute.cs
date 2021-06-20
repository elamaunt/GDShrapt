namespace GDShrapt.Reader
{
    public class GDExtendsAtribute : GDClassAtribute,
        IKeywordReceiver<GDExtendsKeyword>,
        ITypeReceiver,
        IStringReceiver
    {
        internal GDExtendsKeyword ExtendsKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDType Type
        {
            get => (GDType)_form.Token1;
            set => _form.Token1 = value;
        }
        public GDString Path
        {
            get => (GDString)_form.Token1;
            set => _form.Token1 = value;
        }

        enum State
        {
            Extends,
            Path, 
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDExtendsKeyword, GDSimpleSyntaxToken> _form;
        internal override GDTokensForm Form => _form;
        public GDExtendsAtribute()
        {
            _form = new GDTokensForm<State, GDExtendsKeyword, GDSimpleSyntaxToken>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Extends:
                    this.ResolveKeyword(c, state);
                    break;
                case State.Path:
                    this.ResolveString(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                default:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        void IKeywordReceiver<GDExtendsKeyword>.HandleReceivedToken(GDExtendsKeyword token)
        {
            if (_form.State == State.Extends)
            {
                _form.State = State.Path;
                ExtendsKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDExtendsKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Extends)
            {
                _form.State = State.Path;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                Type = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedTypeSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStringReceiver.HandleReceivedToken(GDString token)
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Completed;
                Path = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStringReceiver.HandleReceivedStringSkip()
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Type;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}