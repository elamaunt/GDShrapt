namespace GDShrapt.Reader
{
    public sealed class GDParameterDeclaration : GDNode,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDTypeNode>,
        ITokenOrSkipReceiver<GDAssign>,
        ITokenOrSkipReceiver<GDExpression>
    {
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDTypeNode Type
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDAssign Assign
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDExpression DefaultValue
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public enum State
        {
            Identifier,
            Colon,
            Type,
            Assign,
            DefaultValue,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression> TypedForm => _form;
        public GDParameterDeclaration()
        {
            _form = new GDTokensForm<State, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                case State.Assign:
                    this.ResolveAssign(c, state);
                    break;
                case State.DefaultValue:
                    this.ResolveExpression(c, state);
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
            return new GDParameterDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Colon;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Type;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Assign;
                Type = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.IsOrLowerState(State.Assign))
            {
                _form.State = State.DefaultValue;
                Assign = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Assign))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.DefaultValue))
            {
                _form.State = State.Completed;
                DefaultValue = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.DefaultValue))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}