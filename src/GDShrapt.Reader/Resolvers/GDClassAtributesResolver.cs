using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassAtributesResolver : GDIntendedResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IClassAtributesReceiver Owner { get; }

        public GDClassAtributesResolver(IClassAtributesReceiver owner, int lineIntendation)
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
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassNewLine();
            }
            else
            {
                ResetIntendation();
                ResetSequence();
            }
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void Complete(GDReadingState state, string sequence)
        {
            (GDClassAtribute atribute, bool valid) x;

            x = GetAtribute(sequence);

            if (x.valid)
            {
                if (x.atribute == null)
                    return;

                Owner.HandleReceivedToken(x.atribute);
                state.Push(x.atribute);
            }
            else
            {
                state.Pop();

                for (int i = 0; i < sequence.Length; i++)
                    state.PassChar(sequence[i]);
            }
        }

        private (GDClassAtribute, bool) GetAtribute(string sequence)
        {
            switch (sequence)
            {
                case "class_name":
                    return (new GDClassNameAtribute(), true);
                case "extends":
                    return (new GDExtendsAtribute(), true);
                case "tool":
                    return (new GDToolAtribute(), true);
            default:
                    return (null, false);
            }
        }
    }
}