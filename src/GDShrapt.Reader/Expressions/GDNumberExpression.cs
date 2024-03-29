﻿namespace GDShrapt.Reader
{
    public sealed class GDNumberExpression : GDExpression,
        ITokenOrSkipReceiver<GDNumber>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDNumber Number
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Number,
            Completed
        }

        readonly GDTokensForm<State, GDNumber> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDNumber> TypedForm => _form;
        public GDNumberExpression()
        {
            _form = new GDTokensForm<State, GDNumber>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Number))
            {
                if (IsNumberStartChar(c))
                {
                    _form.State = State.Completed;
                    state.Push(Number = new GDNumber());
                    state.PassChar(c);
                }
                else
                {
                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsNumberStartChar(x) || x == '\n')));
                    state.PassChar(c);
                }
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDNumberExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDNumber>.HandleReceivedToken(GDNumber token)
        {
            if (_form.IsOrLowerState(State.Number))
            {
                _form.State = State.Completed;
                Number = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDNumber>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Number))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
