namespace GDShrapt.Reader
{
    public sealed class GDMatchCaseVariableExpression : GDExpression,
        IKeywordReceiver<GDVarKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.MatchCaseVariable);

        internal GDVarKeyword VarKeyword 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        enum State
        {
            Var,
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDVarKeyword, GDIdentifier> _form;
        internal override GDTokensForm Form => _form;
        public GDMatchCaseVariableExpression()
        {
            _form = new GDTokensForm<State, GDVarKeyword, GDIdentifier>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {

            switch (_form.State)
            {
                case State.Var:
                    if (!this.ResolveStyleToken(c, state))
                        state.PushAndPass(new GDKeywordResolver<GDVarKeyword>(this), c);
                    break;
                case State.Identifier:
                    if (this.ResolveStyleToken(c, state))
                        return;

                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Completed;
                        state.PushAndPass(Identifier = new GDIdentifier(), c);
                        return;
                    }

                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsIdentifierStartChar(c))));
                    state.PassChar(c);
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

        void IKeywordReceiver<GDVarKeyword>.HandleReceivedToken(GDVarKeyword token)
        { 
            if (_form.State == State.Var)
            {
                _form.State = State.Identifier;
                VarKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDVarKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Var)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
