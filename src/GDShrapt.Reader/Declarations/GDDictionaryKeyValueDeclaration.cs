namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclaration : GDNode,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDAssign>
    {
        bool _checkedColon;

        public GDExpression Key
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDColon Colon
        {
            get => (GDColon)_form.Token1;
            set => _form.Token1 = value;
        }
        public GDAssign Assign
        {
            get => (GDAssign)_form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression Value
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public enum State
        {
            Key,
            ColonOrAssign,
            Value,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDSingleCharToken, GDExpression> _form;

        public GDDictionaryKeyValueDeclaration()
        {
            _form = new GDTokensForm<State, GDExpression, GDSingleCharToken, GDExpression>(this);
        }

        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDExpression, GDSingleCharToken, GDExpression> TypedForm => _form;
        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Key:
                    this.ResolveExpression(c, state);
                    break;
                case State.ColonOrAssign:
                    if (!_checkedColon)
                        this.ResolveColon(c, state);
                    else
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

        public override GDNode CreateEmptyInstance()
        {
            return new GDDictionaryKeyValueDeclaration();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Key))
            {
                _form.State = State.ColonOrAssign;
                Key = token;
                return;
            }

            if (_form.IsOrLowerState(State.Value))
            {
                _form.State = State.Completed;
                Value = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Key))
            {
                _form.State = State.ColonOrAssign;
                return;
            }

            if (_form.IsOrLowerState(State.Value))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.ColonOrAssign))
            {
                _checkedColon = true;
                _form.State = State.Value;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.ColonOrAssign))
            {
                _checkedColon = true;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.IsOrLowerState(State.ColonOrAssign))
            {
                _form.State = State.Value;
                Assign = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.ColonOrAssign))
            {
                _form.State = State.Value;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
