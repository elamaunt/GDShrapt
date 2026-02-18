using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDMatchCaseDeclaration : GDIntendedNode,
        ITokenOrSkipReceiver<GDExpressionsList>,
        ITokenOrSkipReceiver<GDWhenKeyword>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDStatementsList>
    {
        public GDExpressionsList Conditions 
        {
            get => _form.GetOrInit(0, new GDExpressionsList());
            set => _form.Token0 = value;
        }
        public GDWhenKeyword When
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression GuardCondition
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDColon Colon
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        public GDStatementsList Statements 
        { 
            get => _form.GetOrInit(5, new GDStatementsList(Intendation + 1));
            set => _form.Token5 = value;
        }

        public enum State
        {
            Conditions,
            When,
            GuardCondition,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDExpressionsList, GDWhenKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDExpressionsList, GDWhenKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> TypedForm => _form;
        internal GDMatchCaseDeclaration(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDExpressionsList, GDWhenKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDMatchCaseDeclaration()
        {
            _form = new GDTokensForm<State, GDExpressionsList, GDWhenKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Conditions:
                    _form.State = State.When;
                    state.PushAndPass(Conditions, c);
                    break;
                case State.When:
                    this.ResolveKeyword<GDWhenKeyword>(c, state);
                    break;
                case State.Expression:
                case State.GuardCondition:
                    this.ResolveExpression(c, state, Intendation);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
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
                case State.Conditions:
                case State.When:
                case State.GuardCondition:
                case State.Colon:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Statements);
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Conditions:
                case State.When:
                case State.GuardCondition:
                case State.Colon:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.Push(Statements);
                    state.PassCarriageReturnChar();
                    break;
                default:
                    state.PopAndPassCarriageReturnChar();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDMatchCaseDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        public override IEnumerable<GDIdentifier> GetMethodScopeDeclarations(int? beforeLine = null)
        {
            return Conditions.AllNodes
                .OfType<GDMatchCaseVariableExpression>()
                .Select(x => x.Identifier)
                .Where(x => x != null);
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Statements;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpressionsList>.HandleReceivedToken(GDExpressionsList token)
        {
            if (_form.IsOrLowerState(State.Conditions))
            {
                _form.State = State.When;
                Conditions = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpressionsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Conditions))
            {
                _form.State = State.When;
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

        void ITokenReceiver<GDWhenKeyword>.HandleReceivedToken(GDWhenKeyword token)
        {
            if (_form.IsOrLowerState(State.When))
            {
                _form.State = State.GuardCondition;
                When = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDWhenKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.When))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.GuardCondition))
            {
                _form.State = State.Colon;
                GuardCondition = token;
                return;
            }

            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.GuardCondition))
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
    }
}
