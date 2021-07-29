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
            if (char.IsLetter(c) || c == '_' || (_sequence.Length > 0 && char.IsDigit(c)))
            {
                _sequence.Append(c);
                return;
            }

            if (_sequence.Length > 0)
            {
                HandleSequence(_sequence.ToString(), state);
                state.PassChar(c);
                return;
            }

            if (IsSpace(c))
            {
                state.PushAndPass(_lastSpace = new GDSpace(), c);
                return;
            }

            Owner.HandleReceivedToken(state.PushAndPass(new GDStatementsList(), c));
        }

        private void HandleSequence(string seq, GDReadingState state)
        {
            _sequence.Clear();

            if (CalculatedIntendation > 0)
            {
                switch (seq)
                {
                    case "var":
                    case "func":
                    case "signal":
                    case "const":
                    case "class":
                    case "static":
                    case "onready":
                        Owner.HandleReceivedToken(state.Push(new GDInnerClassDeclaration(CalculatedIntendation)));
                        break;
                    default:
                        Owner.HandleReceivedToken(state.Push(new GDStatementsList()));
                        break;
                }

                PassIntendationSequence(state);
            }
            else
            {
                switch (seq)
                {
                    case "extends":
                    case "class_name":
                    case "tool":
                    case "var":
                    case "func":
                    case "const":
                    case "signal":
                    case "export":
                    case "class":
                    case "static":
                    case "onready":
                        Owner.HandleReceivedToken(state.Push(new GDClassDeclaration()));
                        break;
                    default:
                        Owner.HandleReceivedToken(state.Push(new GDStatementsList()));
                        break;
                }
            }

            if (_lastSpace != null)
            {
                state.PassString(_lastSpace.ToString());
                _lastSpace = null;
            }

            ResetIntendation();
            state.PassString(seq);
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
                HandleSequence(_sequence.ToString(), state);
                state.PassNewLine();
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
                HandleSequence(_sequence.ToString(), state);
                state.PassSharpChar();
            }
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_sequence.Length > 0)
            {
                HandleSequence(_sequence.ToString(), state);
            }
            else
            {
                if (_lastSpace != null)
                {
                    Owner.HandleReceivedToken(_lastSpace);
                    _lastSpace = null;
                }
            }

            base.ForceComplete(state);
        }
    }
}
