namespace GDShrapt.Reader
{
    public sealed class GDEnumValueDeclaration : GDNode,
        IIdentifierReceiver,
        ITokenReceiver<GDColon>,
        ITokenReceiver<GDAssign>,
        IExpressionsReceiver
    {
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        internal GDColon Colon
        {
            get => (GDColon)_form.Token1;
            set => _form.Token1 = value;
        }

        internal GDAssign Assign
        {
            get => (GDAssign)_form.Token1;
            set => _form.Token1 = value;
        }

        public GDExpression Value
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            Identifier,
            Colon,
            Assign,
            Value,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier, GDSingleCharToken, GDExpression> _form = new GDTokensForm<State, GDIdentifier, GDSingleCharToken, GDExpression>();
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
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Assign:
                    this.ResolveAssign(c, state);
                    break;
                case State.Value:
                    this.ResolveExpression(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.AddBeforeActiveToken(new GDNewLine());
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                Identifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Value;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.Value;
                Assign = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.Value;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Value)
            {
                _form.State = State.Completed;
                Value = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Value)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}