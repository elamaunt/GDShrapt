using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public sealed class GDIdentifierExpression : GDExpression,
        ITokenOrSkipReceiver<GDIdentifier>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Identifier);
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDIdentifier> TypedForm => _form;
        public GDIdentifierExpression()
        {
            _form = new GDTokensForm<State, GDIdentifier>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Completed;
                state.Push(Identifier = new GDIdentifier());
                state.PassChar(c);
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
            return new GDIdentifierExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        public override IEnumerable<GDIdentifier> GetDependencies()
        {
            var identifier = Identifier;

            if (identifier != null)
                yield return identifier;
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Completed;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}