namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a raw string literal in GDScript: r"text" or r'text'.
    /// Raw strings do not process escape sequences.
    /// </summary>
    public sealed class GDRawStringExpression : GDExpression,
        ITokenOrSkipReceiver<GDRawStringPrefix>,
        ITokenOrSkipReceiver<GDStringNode>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);

        /// <summary>
        /// The 'r' prefix token.
        /// </summary>
        public GDRawStringPrefix RawPrefix
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        /// <summary>
        /// The string content of the raw string.
        /// </summary>
        public GDStringNode String
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        /// <summary>
        /// Gets the string sequence (value) of this raw string.
        /// </summary>
        public string Sequence => String?.Sequence;

        public enum State
        {
            RawPrefix,
            String,
            Completed
        }

        readonly GDTokensForm<State, GDRawStringPrefix, GDStringNode> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDRawStringPrefix, GDStringNode> TypedForm => _form;

        public GDRawStringExpression()
        {
            _form = new GDTokensForm<State, GDRawStringPrefix, GDStringNode>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.RawPrefix:
                    if (this.ResolveSpaceToken(c, state))
                        return;

                    if (c == 'r')
                    {
                        _form.State = State.String;
                        RawPrefix = new GDRawStringPrefix();
                        return;
                    }

                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => x == 'r' || x == '\n')));
                    state.PassChar(c);
                    return;

                case State.String:
                    if (this.ResolveSpaceToken(c, state))
                        return;

                    if (IsStringStartChar(c))
                    {
                        state.PushAndPass(new GDRawStringNodeResolver(this), c);
                        return;
                    }

                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsStringStartChar(x) || x == '\n')));
                    state.PassChar(c);
                    return;

                default:
                    state.PopAndPass(c);
                    return;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDRawStringExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDRawStringPrefix>.HandleReceivedToken(GDRawStringPrefix token)
        {
            if (_form.State == State.RawPrefix)
            {
                RawPrefix = token;
                _form.State = State.String;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDRawStringPrefix>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.RawPrefix)
            {
                _form.State = State.String;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStringNode>.HandleReceivedToken(GDStringNode token)
        {
            if (_form.State == State.String)
            {
                String = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStringNode>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.String)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
