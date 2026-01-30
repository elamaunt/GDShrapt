namespace GDShrapt.Reader
{
    internal class GDDictionaryKeyValueResolver : GDResolver, ITokenOrSkipReceiver<GDExpression>
    {
        readonly int _lineIntendationInSpaces;
        bool _keyExpressionChecked;

        ITokenReceiver _receiver;
        GDDictionaryKeyValueDeclaration _activeDeclaration;

        private new ITokenOrSkipReceiver<GDDictionaryKeyValueDeclaration> Owner { get; }
        public INewLineReceiver NewLineReceiver { get; }

        public GDDictionaryKeyValueResolver(ITokenOrSkipReceiver<GDDictionaryKeyValueDeclaration> owner, INewLineReceiver newLineReceiver, int lineIntendationInSpaces) 
            : base(owner)
        {
            _lineIntendationInSpaces = lineIntendationInSpaces;
            Owner = owner;
            NewLineReceiver = newLineReceiver;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_keyExpressionChecked)
            {
                this.ResolveExpression(c, state, _lineIntendationInSpaces, NewLineReceiver, allowAssignment: false);
                return;
            }

            if (_activeDeclaration == null)
            {
                Owner.HandleReceivedTokenSkip();
                state.PopAndPass(c);
            }
            else
            {
                state.Pop();
                Owner.HandleReceivedToken(_activeDeclaration);
                state.PushAndPass(_activeDeclaration, c);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_keyExpressionChecked)
            {
                NewLineReceiver.HandleReceivedToken(new GDNewLine());
                return;
            }

            if (_activeDeclaration == null)
            {
                Owner.HandleReceivedTokenSkip();
                state.PopAndPassNewLine();
            }
            else
            {
                state.Pop();
                Owner.HandleReceivedToken(_activeDeclaration);
                state.PushAndPassNewLine(_activeDeclaration);
            }
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (!_keyExpressionChecked)
            {
                Owner.HandleReceivedToken(new GDCarriageReturnToken());
                return;
            }

            if (_activeDeclaration == null)
            {
                Owner.HandleReceivedTokenSkip();
                state.PopAndPassCarriageReturnChar();
            }
            else
            {
                state.Pop();
                Owner.HandleReceivedToken(_activeDeclaration);
                _activeDeclaration.HandleCarriageReturnChar(state);
            }
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            _activeDeclaration = new GDDictionaryKeyValueDeclaration();
            _activeDeclaration.Add(token);
            _receiver = _activeDeclaration;
            _keyExpressionChecked = true;
        }

        bool ITokenReceiver.IsCompleted => _keyExpressionChecked = true;

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            _keyExpressionChecked = true;
        }

        public void HandleReceivedToken(GDComment token)
        {
            if (_activeDeclaration == null)
                Owner.HandleReceivedToken(token);
            else
                _activeDeclaration.Add(token);
        }

        public void HandleReceivedToken(GDAttribute token)
        {
            if (_activeDeclaration == null)
                Owner.HandleReceivedToken(token);
            else
                _receiver.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDSpace token)
        {
            if ( _activeDeclaration == null)
                Owner.HandleReceivedToken(token);
            else
                _receiver.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDInvalidToken token)
        {
            if (_activeDeclaration == null)
                Owner.HandleReceivedToken(token);
            else
                _receiver.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDMultiLineSplitToken token)
        {
            if (_activeDeclaration == null)
                Owner.HandleReceivedToken(token);
            else
                _receiver.HandleReceivedToken(token);
        }

        public void HandleReceivedToken(GDCarriageReturnToken token)
        {
            if (_activeDeclaration == null)
                Owner.HandleReceivedToken(token);
            else
                _receiver.HandleReceivedToken(token);
        }
    }
}
