namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a StringName literal in GDScript: &amp;"name" or &amp;'name'.
    /// StringName is an immutable string used for fast comparisons (hashed).
    /// </summary>
    public sealed class GDStringNameExpression : GDExpression,
        ITokenOrSkipReceiver<GDAmpersand>,
        ITokenOrSkipReceiver<GDStringNode>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);

        /// <summary>
        /// The &amp; prefix operator.
        /// </summary>
        public GDAmpersand Ampersand
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        /// <summary>
        /// The string content of the StringName.
        /// </summary>
        public GDStringNode String
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        /// <summary>
        /// Gets the string sequence (value) of this StringName.
        /// Convenience property that returns the inner string's sequence.
        /// </summary>
        public string Sequence => String?.Sequence;

        public enum State
        {
            Ampersand,
            String,
            Completed
        }

        readonly GDTokensForm<State, GDAmpersand, GDStringNode> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDAmpersand, GDStringNode> TypedForm => _form;

        public GDStringNameExpression()
        {
            _form = new GDTokensForm<State, GDAmpersand, GDStringNode>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Ampersand:
                    if (this.ResolveSpaceToken(c, state))
                        return;

                    if (c == '&')
                    {
                        _form.State = State.String;
                        Ampersand = new GDAmpersand();
                        return;
                    }

                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => x == '&' || x == '\n')));
                    state.PassChar(c);
                    return;

                case State.String:
                    if (this.ResolveSpaceToken(c, state))
                        return;

                    if (IsStringStartChar(c))
                    {
                        this.ResolveString(c, state);
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
            return new GDStringNameExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDAmpersand>.HandleReceivedToken(GDAmpersand token)
        {
            if (_form.State == State.Ampersand)
            {
                Ampersand = token;
                _form.State = State.String;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAmpersand>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Ampersand)
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
