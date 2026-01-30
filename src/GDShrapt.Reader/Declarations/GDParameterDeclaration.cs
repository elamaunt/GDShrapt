namespace GDShrapt.Reader
{
    public sealed class GDParameterDeclaration : GDNode,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDTypeNode>,
        ITokenOrSkipReceiver<GDAssign>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
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
        readonly int _intendation;

        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression> TypedForm => _form;
        internal GDParameterDeclaration(int intendation)
        {
            _form = new GDTokensForm<State, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression>(this);
            _intendation = intendation;
        }

        public GDParameterDeclaration()
        {
            _form = new GDTokensForm<State, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Identifier:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    if (IsSpace(c))
                    {
                        // Buffer spaces - they may be trailing
                        if (!state.IsRepassingChars)
                        {
                            state.AddPendingChar(c);
                            return;
                        }
                        // On repass - use standard space token handling (accumulates into one GDSpace)
                        this.ResolveSpaceToken(c, state);
                        return;
                    }
                    // Non-space - determine if it's colon, assign, or stop char
                    if (c == ':' || c == '=')
                    {
                        // Continuation - spaces go to form
                        state.RepassPendingChars();
                        // Complete any GDSpace that was created during repass
                        state.CompleteActiveCharSequence();
                        this.ResolveColon(c, state);
                    }
                    else
                    {
                        // Stop char - spaces are trailing, pass to parent
                        _form.State = State.Completed;
                        state.Pop();
                        state.RepassPendingChars();
                        state.PassChar(c);
                    }
                    break;
                case State.Type:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveType(c, state);
                    break;
                case State.Assign:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveAssign(c, state);
                    break;
                case State.DefaultValue:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveExpression(c, state, _intendation, this);
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

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            _form.AddBeforeActiveToken(new GDCarriageReturnToken());
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            // Spaces before comment are not trailing, they're part of formatting
            state.RepassPendingChars();
            base.HandleSharpChar(state);
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

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            if (_form.State != State.Completed)
            {
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            if (_form.State != State.Completed)
            {
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}