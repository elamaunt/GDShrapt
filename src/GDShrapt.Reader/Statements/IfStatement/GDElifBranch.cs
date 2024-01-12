namespace GDShrapt.Reader
{
    public sealed class GDElifBranch : GDNode,
        ITokenOrSkipReceiver<GDElifKeyword>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDStatementsList>
    {
        public GDElifKeyword ElifKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDExpression Condition
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDColon Colon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDStatementsList Statements
        {
            get => _form.Token4 ?? (_form.Token4 = new GDStatementsList(_intendation + 1));
            set => _form.Token4 = value;
        }

        public enum State
        {
            Elif,
            Condition,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly int _intendation;
        readonly GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDElifBranch(int intendation)
        {
            _intendation = intendation;
            _form = new GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDElifBranch()
        {
            _form = new GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Elif:
                    this.ResolveKeyword<GDElifKeyword>(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Condition:
                case State.Expression:
                    this.ResolveExpression(c, state, _intendation);
                    break;
                case State.Statements:
                    this.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Elif:
                case State.Condition:
                case State.Colon:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.Push(Statements);
                    state.PassNewLine();
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDElifBranch();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDElifKeyword>.HandleReceivedToken(GDElifKeyword token)
        {
            if (_form.IsOrLowerState(State.Elif))
            {
                _form.State = State.Condition;
                ElifKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDElifKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Elif))
            {
                _form.State = State.Condition;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }
        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Condition))
            {
                _form.State = State.Colon;
                Condition = token;
                return;
            }

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
            if (_form.IsOrLowerState(State.Condition))
            {
                _form.State = State.Colon;
                return;
            }

            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStatementsList>.HandleReceivedToken(GDStatementsList token)
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                _form.State = State.Completed;
                Statements = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStatementsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
