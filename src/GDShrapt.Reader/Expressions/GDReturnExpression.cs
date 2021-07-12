namespace GDShrapt.Reader
{
    public sealed class GDReturnExpression : GDExpression, 
        ITokenOrSkipReceiver<GDExpression>, 
        ITokenOrSkipReceiver<GDReturnKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Return);
        public GDReturnKeyword ReturnKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDExpression Expression
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public enum State
        {
            Return,
            Expression,
            Completed
        }

        readonly GDTokensForm<State, GDReturnKeyword, GDExpression> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDReturnKeyword, GDExpression> TypedForm => _form;
        public GDReturnExpression()
        {
            _form = new GDTokensForm<State, GDReturnKeyword, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Return:
                    if (!this.ResolveSpaceToken(c, state))
                        state.PushAndPass(new GDKeywordResolver<GDReturnKeyword>(this), c);
                    break;
                case State.Expression:
                    if (!this.ResolveSpaceToken(c, state))
                        state.PushAndPass(new GDExpressionResolver(this), c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDReturnExpression();
        }

        void ITokenReceiver<GDReturnKeyword>.HandleReceivedToken(GDReturnKeyword token)
        {
            if (_form.IsOrLowerState(State.Return))
            {
                _form.State = State.Expression;
                ReturnKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDReturnKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Return))
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Completed;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
