using System;

namespace GDShrapt.Reader
{
    internal class GDTypeResolver : GDResolver, ITokenOrSkipReceiver<GDStringNode>
    {
        GDType _type;
        GDStringNode _string;
        GDSpace _space;
        bool _completed;

        public new ITokenOrSkipReceiver<GDTypeNode> Owner { get; }

        public GDTypeResolver(ITokenOrSkipReceiver<GDTypeNode> owner)
            : base(owner)
        {
            Owner = owner;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_type == null && _string == null && c.IsSpace())
            {
                var space = new GDSpace();
                Owner.HandleReceivedToken(space);
                state.PushAndPass(space, c);
                return;
            }

            if (c.IsStringStartChar())
            {
                this.ResolveString(c, state);
                return;
            }

            if (c.IsIdentifierStartChar())
            {
                state.PushAndPass(_type = new GDType(), c);
                return;
            }

            if (c == '[')
            {
                var arrayTypeNode = new GDArrayTypeNode();

                if (_type != null)
                {
                    if (_type.IsArray)
                    {
                        state.Pop();

                        Owner.HandleReceivedToken(arrayTypeNode);

                        state.Push(arrayTypeNode);

                        var sequence = _type.Sequence;
                        for (int i = 0; i < sequence.Length; i++)
                            state.PassChar(sequence[i]);

                        if (_space != null)
                        {
                            for (int i = 0; i < _space.Sequence.Length; i++)
                                state.PassChar(_space.Sequence[i]);

                            _space = null;
                        }

                        state.PassChar(c);
                        _type = null;
                        return;
                    }
                    else
                    {
                        Owner.HandleReceivedToken(new GDSingleTypeNode() { Type = _type });
                        _type = null;

                        state.Pop();

                        if (_space != null)
                        {
                            for (int i = 0; i < _space.Sequence.Length; i++)
                                state.PassChar(_space.Sequence[i]);

                            _space = null;
                        }

                        state.PassChar(c);
                        return;
                    }
                }

                state.Pop();

                Owner.HandleReceivedToken(arrayTypeNode);

                state.Push(arrayTypeNode);
                state.PassChar(c);
                return;
            }

            if (c.IsSpace())
            {
                state.PushAndPass(_space = new GDSpace(), c);
                return;
            }

            state.Pop();
            Complete(state);
            state.PassChar(c);
        }

        private void Complete(GDReadingState state)
        {
            if (_completed)
                return;

            _completed = true;

            if (_type != null)
            {
                var sequence = _type.Sequence;
                if (sequence.Equals("set", System.StringComparison.Ordinal) ||
                    sequence.Equals("get", System.StringComparison.Ordinal))
                {
                    Owner.HandleReceivedTokenSkip();

                    for (int i = 0; i < sequence.Length; i++)
                        state.PassChar(sequence[i]);
                }
                else
                {
                    Owner.HandleReceivedToken(new GDSingleTypeNode() { Type = _type });
                }

                _type = null;
            }
            else
            {
                if (_string != null)
                {
                    Owner.HandleReceivedToken(new GDStringTypeNode() { Path = _string });
                }
                else
                {
                    Owner.HandleReceivedTokenSkip();
                }

                _string = null;
            }

            if (_space != null)
            {
                for (int i = 0; i < _space.Sequence.Length; i++)
                    state.PassChar(_space.Sequence[i]);

                _space = null;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            Complete(state);
            state.PassNewLine();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            Complete(state);
            state.PassSharpChar();
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            state.Pop();
            Complete(state);
            state.PassLeftSlashChar();
        }

        internal override void ForceComplete(GDReadingState state)
        {
            base.ForceComplete(state);
            Complete(state);
        }

        void ITokenReceiver<GDStringNode>.HandleReceivedToken(GDStringNode token)
        {
            _string = token;
        }

        void ITokenReceiver.HandleReceivedToken(GDAttribute token)
        {
            throw new GDInvalidStateException();
        }
    
        void ITokenSkipReceiver<GDStringNode>.HandleReceivedTokenSkip()
        {
            throw new GDInvalidStateException();
        }

        void ITokenReceiver.HandleReceivedToken(GDComment token)
        {
            throw new GDInvalidStateException();
        }

        void ITokenReceiver.HandleReceivedToken(GDSpace token)
        {
            throw new GDInvalidStateException();
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            throw new GDInvalidStateException();
        }

        void ITokenReceiver.HandleReceivedToken(GDMultiLineSplitToken token)
        {
            throw new GDInvalidStateException();
        }
    }
}
