namespace GDShrapt.Reader
{
    public class GDClassNameAtribute : GDClassAtribute, ITokenReceiver<GDComma>
    {
        public GDIdentifier Identifier 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        internal GDComma Comma
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDString Icon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            Identifier,
            Comma,
            Icon,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier, GDComma, GDString> _form = new GDTokensForm<State, GDIdentifier, GDComma, GDString>();
        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Identifier:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Comma;
                        state.Push(Identifier = new GDIdentifier());
                    }
                    else
                        state.Pop();

                    state.PassChar(c);
                    break;
                case State.Comma:
                    state.Push(new GDSingleCharTokenResolver<GDComma>(this));
                    state.PassChar(c);
                    break;
                case State.Icon:
                    if (IsStringStartChar(c))
                    {
                        _form.State = State.Completed;
                        state.Push(Icon = new GDString());
                    }
                    else
                        state.Pop();

                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            if (_form.State == State.Comma)
            {
                _form.State = State.Icon;
                Comma = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Comma)
            {
                _form.State = State.Icon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}