using System.Linq.Expressions;

namespace GDShrapt.Reader
{
    public sealed class GDAwaitExpression : GDExpression,
        ITokenOrSkipReceiver<GDAwaitKeyword>,
        ITokenOrSkipReceiver<GDExpression>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Await);

        public GDAwaitKeyword AwaitKeyword
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
            Await,
            Expression,
            Completed
        }

        readonly int _intendation;
        readonly GDTokensForm<State, GDAwaitKeyword, GDExpression> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDAwaitKeyword, GDExpression> TypedForm => _form;

        internal GDAwaitExpression(int intendation)
        {
            _intendation = intendation;
            _form = new GDTokensForm<State, GDAwaitKeyword, GDExpression>(this);
        }

        public GDAwaitExpression()
        {
            _form = new GDTokensForm<State, GDAwaitKeyword, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Await:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDAwaitKeyword>(c, state);
                    break;
                case State.Expression:
                    this.ResolveExpression(c, state, _intendation);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.State = State.Completed;
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDAwaitExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDAwaitKeyword>.HandleReceivedToken(GDAwaitKeyword token)
        {
            if (_form.IsOrLowerState(State.Await))
            {
                _form.State = State.Expression;
                AwaitKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAwaitKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Await))
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