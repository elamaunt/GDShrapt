using System.Text;

namespace GDShrapt.Reader
{
    internal class GDContentResolver : GDIntendedResolver
    {
        new IIntendedTokenReceiver<GDNode> Owner { get; }

        readonly StringBuilder _sequence = new StringBuilder();

        GDSpace _lastSpace;

        public GDContentResolver(IIntendedTokenReceiver<GDNode> owner)
            : base(owner, 0)
        {
            Owner = owner;
        }


        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (char.IsLetter(c) || c == '_')
            {
                _sequence.Append(c);
                return;
            }

            if (_sequence.Length > 0)
            {
                HandleSequence(_sequence.ToString());
                _sequence.Clear();
                state.PassChar(c);
                return;
            }

            if (IsSpace(c))
            {
                state.PushAndPass(_lastSpace = new GDSpace(), c);
                return;
            }

            // statements
            //state.Pop();

            state.PushAndPass(new GDStatementsList(), c);


        }

        private void HandleSequence(string seq)
        {

        }

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            if (_sequence.Length == 0)
            {
                if (_lastSpace != null)
                {
                    Owner.HandleReceivedToken(_lastSpace);
                    _lastSpace = null;
                }

                Owner.AddNewLine();
            }
            else
            {
                HandleSequence(_sequence.ToString());
            }
        }

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            if (_sequence.Length == 0)
            {
                if (_lastSpace != null)
                {
                    Owner.HandleReceivedToken(_lastSpace);
                    _lastSpace = null;
                }

                Owner.HandleReceivedToken(state.PushAndPass(new GDComment(), '#'));
            }
            else
            {
                HandleSequence(_sequence.ToString());
            }
        }
    }
}
