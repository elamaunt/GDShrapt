using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassAtributesResolver : GDIntendedResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IIntendedTokenReceiver<GDClassAtribute> Owner { get; }

        public GDClassAtributesResolver(IIntendedTokenReceiver<GDClassAtribute> owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (!IsSpace(c))
            {
                _sequenceBuilder.Append(c);
            }
            else
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassChar(c);
            }
        }

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassNewLine();
                return;
            }

            ResetIntendation();
            state.PassNewLine();
        }

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassSharpChar();
                return;
            }

            Owner.HandleReceivedToken(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void Complete(GDReadingState state, string sequence)
        {
            var atribute = GetAtribute(sequence);

            if (atribute != null)
            {
                SendIntendationTokensToOwner();
                Owner.HandleReceivedToken(atribute);
                state.Push(atribute);

                for (int i = 0; i < sequence.Length; i++)
                    state.PassChar(sequence[i]);
            }
            else
            {
                state.Pop();

                PassIntendationSequence(state);

                for (int i = 0; i < sequence.Length; i++)
                    state.PassChar(sequence[i]);
            }
        }

        private GDClassAtribute GetAtribute(string sequence)
        {
            switch (sequence)
            {
                case "class_name":
                    return new GDClassNameAtribute();
                case "extends":
                    return new GDExtendsAtribute();
                case "tool":
                    return new GDToolAtribute();
            default:
                    return null;
            }
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                ResetIntendation();
                return;
            }

            SendIntendationTokensToOwner();
            base.ForceComplete(state);
        }
    }
}