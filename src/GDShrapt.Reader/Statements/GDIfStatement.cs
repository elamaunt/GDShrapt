namespace GDShrapt.Reader
{
    public sealed class GDIfStatement : GDStatement,
        IIntendedTokenOrSkipReceiver<GDIfBranch>,
        IIntendedTokenOrSkipReceiver<GDElseBranch>
    {
        bool _waitForEndLine = true;

        public GDIfBranch IfBranch
        {
            get => _form.Token0 ?? (_form.Token0 = new GDIfBranch(LineIntendation));
            set => _form.Token0 = value;
        }
        public GDElifBranchesList ElifBranchesList
        {
            get => _form.Token1 ?? (_form.Token1 = new GDElifBranchesList(LineIntendation));
            set => _form.Token1 = value;
        }
        public GDElseBranch ElseBranch
        {
            get => _form.Token2 ?? (_form.Token2 = new GDElseBranch(LineIntendation));
            set => _form.Token2 = value;
        }

        enum State
        {
            IfBranch,
            ElifBranches,
            ElseBranch,
            Completed
        }

        readonly GDTokensForm<State, GDIfBranch, GDElifBranchesList, GDElseBranch> _form;
        public override GDTokensForm Form => _form;

        internal GDIfStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDIfBranch, GDElifBranchesList, GDElseBranch>(this);
        }

        public GDIfStatement()
        {
            _form = new GDTokensForm<State, GDIfBranch, GDElifBranchesList, GDElseBranch>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.IfBranch:
                    _form.State = State.ElifBranches;
                    state.PushAndPass(IfBranch, c);
                    break;
                case State.ElifBranches:
                    _form.State = State.ElseBranch;
                    state.PushAndPass(ElifBranchesList, c);
                    break;
                case State.ElseBranch:
                    state.PushAndPassNewLine(new GDElseResolver(this, LineIntendation));
                    break;
                default:
                    if (!this.ResolveSpaceToken(c, state))
                    {
                        if (_waitForEndLine)
                            this.ResolveInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                        else
                            state.PopAndPass(c);
                    }
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.IfBranch:
                    _form.State = State.ElifBranches;
                    state.PushAndPassNewLine(IfBranch);
                    break;
                case State.ElifBranches:
                    _form.State = State.ElseBranch;
                    state.PushAndPassNewLine(ElifBranchesList);
                    break;
                case State.ElseBranch:
                    state.PushAndPassNewLine(new GDElseResolver(this, LineIntendation));
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDIfStatement();
        }

        void ITokenReceiver<GDIfBranch>.HandleReceivedToken(GDIfBranch token)
        {
            if (_form.IsOrLowerState(State.IfBranch))
            {
                IfBranch = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIfBranch>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.IfBranch))
                return;

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDElseBranch>.HandleReceivedToken(GDElseBranch token)
        {
            if (_form.IsOrLowerState(State.ElseBranch))
            {
                ElseBranch = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDElseBranch>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.ElseBranch))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void IIntendedTokenReceiver.HandleReceivedToken(GDIntendation token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(token);
        }
    }
}