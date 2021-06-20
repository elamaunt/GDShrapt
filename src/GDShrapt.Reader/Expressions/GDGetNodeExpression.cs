using System;

namespace GDShrapt.Reader
{
    public sealed class GDGetNodeExpression : GDExpression,
        ITokenReceiver<GDDollar>,
        IPathReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.GetNode);

        internal GDDollar Dollar
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDPath Path
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        enum State
        {
            Dollar,
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDDollar, GDPath> _form;
        internal override GDTokensForm Form => _form;
        public GDGetNodeExpression()
        {
            _form = new GDTokensForm<State, GDDollar, GDPath>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Dollar:
                    if (this.ResolveStyleToken(c, state))
                        return;
                    this.ResolveDollar(c, state);
                    break;
                case State.Path:
                    this.ResolvePath(c, state);
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

        void ITokenReceiver<GDDollar>.HandleReceivedToken(GDDollar token)
        {
            if (_form.State == State.Dollar)
            {
                _form.State = State.Path;
                Dollar = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDDollar>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Dollar)
            {
                _form.State = State.Path;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IPathReceiver.HandleReceivedToken(GDPath token)
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Completed;
                Path = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IPathReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
