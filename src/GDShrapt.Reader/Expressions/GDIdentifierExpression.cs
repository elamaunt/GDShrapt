using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public sealed class GDIdentifierExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Identifier);
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier> _form;
        public override GDTokensForm Form => _form;
        public GDIdentifierExpression()
        {
            _form = new GDTokensForm<State, GDIdentifier>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Identifier)
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

        public override IEnumerable<GDIdentifier> GetDependencies()
        {
            var identifier = Identifier;

            if (identifier != null)
                yield return identifier;
        }
    }
}