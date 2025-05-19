namespace GDShrapt.Reader
{
    public sealed class GDGetUniqueNodeExpression : GDExpression,
        ITokenOrSkipReceiver<GDPercent>,
        ITokenOrSkipReceiver<GDExternalName>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.GetUniqueNode);

        public GDPercent Percent
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDExternalName Name 
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public enum State
        {
            Percent,
            Name,
            Completed
        }

        readonly GDTokensForm<State, GDPercent, GDExternalName> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDPercent, GDExternalName> TypedForm => _form;
        public GDGetUniqueNodeExpression()
        {
            _form = new GDTokensForm<State, GDPercent, GDExternalName>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Percent:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolvePercent(c, state);
                    break;
                case State.Name:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveExternalName(c, state);
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
            return new GDGetUniqueNodeExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDPercent>.HandleReceivedToken(GDPercent token)
        {
            if (_form.IsOrLowerState(State.Percent))
            {
                _form.State = State.Name;
                Percent = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPercent>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Percent))
            {
                _form.State = State.Name;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExternalName>.HandleReceivedToken(GDExternalName token)
        {
            if (_form.IsOrLowerState(State.Name))
            {
                _form.State = State.Completed;
                Name = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExternalName>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Name))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
