using System.Text;

namespace GDShrapt.Reader
{
    internal class GDContentResolver : GDResolver
    {
        new IIntendedTokenReceiver<GDNode> Owner { get; }

        readonly StringBuilder _sequence = new StringBuilder();

        GDSpace _lastSpace;

        public GDContentResolver(IIntendedTokenReceiver<GDNode> owner)
            : base(owner)
        {
            Owner = owner;
        }


        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                if (_sequence.Length == 0)
                {
                    state.PushAndPass(_lastSpace = new GDSpace(), c);
                }
                else
                {
                    HandleSequence(_sequence.ToString());
                }
            }
            else
            {
                //if (char.

                if (char.IsLetter(c))
                {
                    _sequence.Append(c);
                    return;
                }
            }
        }

        private void HandleSequence(string seq)
        {

        }

        internal override void HandleNewLineChar(GDReadingState state)
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

        internal override void HandleSharpChar(GDReadingState state)
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
