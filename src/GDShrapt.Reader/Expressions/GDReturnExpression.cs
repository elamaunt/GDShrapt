namespace GDShrapt.Reader
{
    public sealed class GDReturnExpression : GDExpression, 
        IExpressionsReceiver, 
        IKeywordReceiver<GDReturnKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Return);
        internal GDReturnKeyword ReturnKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDExpression Expression
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        enum State
        {
            Return,
            Expression,
            Completed
        }

        readonly GDTokensForm<State, GDReturnKeyword, GDExpression> _form = new GDTokensForm<State, GDReturnKeyword, GDExpression>();
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
                case State.Return:
                    state.Push(new GDKeywordResolver<GDReturnKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Expression:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        void IKeywordReceiver<GDReturnKeyword>.HandleReceivedToken(GDReturnKeyword token)
        {
            if (_form.State == State.Return)
            {
                _form.State = State.Expression;
                ReturnKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDReturnKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Return)
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.Completed;
                Expression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
