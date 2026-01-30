using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDPathList : GDSeparatedList<GDLayersList, GDRightSlash>,
        ITokenReceiver<GDSpace>,
        ITokenOrSkipReceiver<GDLayersList>,
        ITokenOrSkipReceiver<GDRightSlash>
    {
        bool _switch;
        bool _started;
        bool _ended;

        public GDPathBoundingChar BoundingChar { get; set; }
        public bool StartedWithSlash { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_started && !_ended)
            {
                if (c == '/' && Form.Count == 0)
                {
                    StartedWithSlash = true;
                    return;
                }

                if ((c.IsExpressionStopChar() || c == '.') && BoundingChar == GDPathBoundingChar.None)
                {
                    _ended = true;
                    state.PopAndPass(c);
                    return;
                }

                if (c == '\'' && BoundingChar == GDPathBoundingChar.SingleQuotas)
                {
                    _ended = true;
                    state.Pop();
                    return;
                }

                if (c == '"' && BoundingChar == GDPathBoundingChar.DoubleQuotas)
                {
                    _ended = true;
                    state.Pop();
                    return;
                }
            }

            if (!_started)
            {
                if (c == '/')
                {
                    StartedWithSlash = true;
                    _started = true;
                    return;
                }

                if (c == '\'')
                {
                    BoundingChar = GDPathBoundingChar.SingleQuotas;
                    _started = true;
                    return;
                }

                if (c == '"')
                {
                    BoundingChar = GDPathBoundingChar.DoubleQuotas;
                    _started = true;
                    return;
                }

                _started = true;
            }

            if (this.ResolveSpaceToken(c, state))
                return;

            if (_ended)
            {
                state.PopAndPass(c);
                return;
            }

            if (!_switch)
                this.ResolveLayersList(c, state, BoundingChar != GDPathBoundingChar.None);
            else
                this.ResolveRightSlash(c, state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
           _ended = true;
            state.PopAndPassNewLine();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            _ended = true;
            state.PopAndPassCarriageReturnChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDPathList();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDLayersList>.HandleReceivedToken(GDLayersList token)
        {
            _switch = !_switch;
            ListForm.AddToEnd(token);
        }

        void ITokenReceiver<GDRightSlash>.HandleReceivedToken(GDRightSlash token)
        {
            _switch = !_switch;
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDLayersList>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenSkipReceiver<GDRightSlash>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenReceiver<GDSpace>.HandleReceivedToken(GDSpace token)
        {
            ListForm.AddToEnd(token);
        }

        public override void AppendTo(StringBuilder builder)
        {
            switch (BoundingChar)
            {
                case GDPathBoundingChar.None:
                    if (StartedWithSlash)
                        builder.Append('/');
                    base.AppendTo(builder);
                    break;
                case GDPathBoundingChar.SingleQuotas:
                    builder.Append('\'');
                    if (StartedWithSlash)
                        builder.Append('/');
                    base.AppendTo(builder);
                    if (_ended)
                        builder.Append('\'');
                    break;
                case GDPathBoundingChar.DoubleQuotas:
                    builder.Append('"');
                    if (StartedWithSlash)
                        builder.Append('/');
                    base.AppendTo(builder);
                    if (_ended)
                        builder.Append('"');
                    break;
                default:
                    if (StartedWithSlash)
                        builder.Append('/');
                    base.AppendTo(builder);
                    break;
            }
        }
    }
}
