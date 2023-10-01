namespace GDShrapt.Reader
{
    public sealed class GDElseBranch : GDNode,
        ITokenOrSkipReceiver<GDElseKeyword>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDStatementsList>
    {
        public GDElseKeyword ElseKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDStatementsList Statements
        {
            get => _form.Token3 ?? (_form.Token3 = new GDStatementsList(_intendation + 1));
            set => _form.Token3 = value;
        }

        public enum State
        {
            Else, 
            Colon,
            Expression,
            Statements,
            Completed
        }

        private readonly int _intendation;
        readonly GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDElseBranch(int intendation)
        {
            _intendation = intendation;
            _form = new GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDElseBranch()
        {
            _form = new GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Else:
                    this.ResolveKeyword<GDElseKeyword>(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Expression:
                    this.ResolveExpression(c, state);
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
                case State.Else:
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
            return new GDElseBranch();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDElseKeyword>.HandleReceivedToken(GDElseKeyword token)
        {
            if (_form.IsOrLowerState(State.Else))
            {
                _form.State = State.Colon;
                ElseKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDElseKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Else))
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
