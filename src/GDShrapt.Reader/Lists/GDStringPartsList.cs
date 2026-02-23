namespace GDShrapt.Reader
{
    public class GDStringPartsList : GDSeparatedList<GDStringPart, GDMultiLineSplitToken>, 
        ITokenOrSkipReceiver<GDStringPart>,
        ITokenOrSkipReceiver<GDMultiLineSplitToken>
    {
        bool _firstSlashChecking;
        bool _ended;
        readonly GDStringBoundingChar _bounder;
        readonly bool _isRawString;

        public GDStringPartsList()
        {
        }

        internal GDStringPartsList(GDStringBoundingChar bounder, bool isRawString = false)
        {
            _bounder = bounder;
            _isRawString = isRawString;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_ended)
            {
                state.PopAndPass(c);
                return;
            }

            this.ResolveStringPart(c, state, _bounder, _isRawString);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_ended)
            {
                state.PopAndPassNewLine();
                return;
            }

            this.ResolveStringPart('\n', state, _bounder, _isRawString);
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (_ended)
            {
                state.PopAndPassCarriageReturnChar();
                return;
            }

            this.ResolveStringPart('\r', state, _bounder, _isRawString);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            if (_isRawString)
            {
                HandleChar('\\', state);
                return;
            }

            if (Count == 0)
            {
                _firstSlashChecking = true;
                this.ResolveStringPart('\\', state, _bounder);
                return;
            }

            _ended = false;
            ListForm.AddToEnd(state.Push(new GDMultiLineSplitToken()));
            state.PassLeftSlashChar();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDStringPartsList();
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        void ITokenReceiver<GDStringPart>.HandleReceivedToken(GDStringPart token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDStringPart>.HandleReceivedTokenSkip()
        {
            if (!_firstSlashChecking)
                _ended = true;
            else
                _firstSlashChecking = false;
        }

        void ITokenReceiver<GDMultiLineSplitToken>.HandleReceivedToken(GDMultiLineSplitToken token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDMultiLineSplitToken>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }
    }
}
