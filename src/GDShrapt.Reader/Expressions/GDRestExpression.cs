namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents the ".." rest/spread expression used in dictionary and array patterns.
    /// This expression captures remaining elements in pattern matching.
    /// </summary>
    public sealed class GDRestExpression : GDExpression,
        ITokenOrSkipReceiver<GDDoubleDot>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Rest);

        public GDDoubleDot DoubleDot
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            DoubleDot,
            Completed
        }

        readonly GDTokensForm<State, GDDoubleDot> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDDoubleDot> TypedForm => _form;

        public GDRestExpression()
        {
            _form = new GDTokensForm<State, GDDoubleDot>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDRestExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDDoubleDot>.HandleReceivedToken(GDDoubleDot token)
        {
            if (_form.IsOrLowerState(State.DoubleDot))
            {
                _form.State = State.Completed;
                DoubleDot = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDDoubleDot>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.DoubleDot))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
