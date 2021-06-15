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

        readonly GDTokensForm<State, GDVarKeyword, GDIdentifier> _form = new GDTokensForm<State, GDVarKeyword, GDIdentifier>();
        internal override GDTokensForm Form => _form;

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
                case State.Var:
                    state.Push(new GDKeywordResolver<GDVarKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Identifier:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Completed;
                        state.Push(Identifier = new GDIdentifier());
                        state.PassChar(c);
                        return;
                    }

                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsIdentifierStartChar(c))));
                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
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
