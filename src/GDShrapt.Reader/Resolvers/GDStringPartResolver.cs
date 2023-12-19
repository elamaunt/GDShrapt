using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStringPartResolver : GDResolver
    {
        readonly GDStringBoundingChar _bounder;
        readonly bool _multiline;

        int _boundingCharsCounter;
        bool _escapeNextChar;

        new ITokenOrSkipReceiver<GDStringPart> Owner { get; }

        readonly StringBuilder _stringBuilder = new StringBuilder();

        public GDStringPartResolver(ITokenOrSkipReceiver<GDStringPart> owner, GDStringBoundingChar bounder, bool multilne) 
            : base(owner)
        {
            Owner = owner;
            _bounder = bounder;
            _multiline = multilne;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_escapeNextChar)
            {
                // https://docs.godotengine.org/en/stable/classes/class_string.html

                switch (c)
                {
                    case '\'': break;
                    case '"': break;
                    case '\\': break;
                    case 'a': break;
                    case 'b': break;
                    case 'f': break;
                    case 'n': break;
                    case 'r': break;
                    case 't': break;
                    case 'v': break;
                    case 'u': break;
                    default:
                        if (_stringBuilder.Length == 0)
                            Owner.HandleReceivedTokenSkip();
                        else
                            Owner.HandleReceivedToken(new GDStringPart() { Sequence = _stringBuilder.ToString() });

                        state.Pop();
                        state.PassLeftSlashChar();
                        state.PassChar(c);
                        return;
                }

                _escapeNextChar = false;
                _stringBuilder.Append('\\');
                _stringBuilder.Append(c);
                return;
            }

            if (_multiline)
            {
                if ((c == '\'' && _bounder == GDStringBoundingChar.SingleQuotas) ||
                    (c == '"' && _bounder == GDStringBoundingChar.DoubleQuotas))
                {
                    _boundingCharsCounter++;

                    if (_boundingCharsCounter == 3)
                    {
                        if (_stringBuilder.Length == 0)
                            Owner.HandleReceivedTokenSkip();
                        else
                            Owner.HandleReceivedToken(new GDStringPart() { Sequence = _stringBuilder.ToString() });

                        state.Pop();

                        if (_bounder == GDStringBoundingChar.SingleQuotas)
                        {
                            for (int i = 0; i < _boundingCharsCounter; i++)
                                state.PassChar('\'');
                        }
                        else
                        {
                            for (int i = 0; i < _boundingCharsCounter; i++)
                                state.PassChar('"');
                        }
                    }

                    return;
                }

                if (_bounder == GDStringBoundingChar.SingleQuotas)
                {
                    for (int i = 0; i < _boundingCharsCounter; i++)
                        _stringBuilder.Append('\'');
                }
                else
                {
                    for (int i = 0; i < _boundingCharsCounter; i++)
                        _stringBuilder.Append('"');
                }

                _boundingCharsCounter = 0;

                _stringBuilder.Append(c);
            }
            else
            {
                if ((c == '\'' && _bounder == GDStringBoundingChar.SingleQuotas) ||
                    (c == '"' && _bounder == GDStringBoundingChar.DoubleQuotas))
                {
                    if (_stringBuilder.Length == 0)
                        Owner.HandleReceivedTokenSkip();
                    else
                        Owner.HandleReceivedToken(new GDStringPart() { Sequence = _stringBuilder.ToString() });
                    state.PopAndPass(c);
                    return;
                }

                _stringBuilder.Append(c);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_bounder == GDStringBoundingChar.SingleQuotas)
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append('\'');
            }
            else
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append('"');
            }
            _boundingCharsCounter = 0;

            if (_multiline || _escapeNextChar)
                HandleChar('\n', state);
            else
            {
                if (_stringBuilder.Length == 0)
                    Owner.HandleReceivedTokenSkip();
                else
                    Owner.HandleReceivedToken(new GDStringPart() { Sequence = _stringBuilder.ToString() });

                state.Pop();

                if (_bounder == GDStringBoundingChar.SingleQuotas)
                {
                    for (int i = 0; i < _boundingCharsCounter; i++)
                        _stringBuilder.Append('\'');
                }
                else
                {
                    for (int i = 0; i < _boundingCharsCounter; i++)
                        _stringBuilder.Append('"');
                }

                state.PassNewLine();
            }
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            if (_bounder == GDStringBoundingChar.SingleQuotas)
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append('\'');
            }
            else
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append('"');
            }
            _boundingCharsCounter = 0;

            if (_escapeNextChar)
            {
                HandleChar('\\', state);
                return;
            }

            _escapeNextChar = true;

            //if (_multiline || _escapeNextChar)
            //    HandleChar('\\', state);
            //else
            //{
            //escapeNextChar = true;

            //    return;

            //}
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            base.ForceComplete(state);

            if (_stringBuilder.Length == 0)
                Owner.HandleReceivedTokenSkip();
            else
                Owner.HandleReceivedToken(new GDStringPart() { Sequence = _stringBuilder.ToString() });

            if (_bounder == GDStringBoundingChar.SingleQuotas)
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append('\'');
            }
            else
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append('"');
            }
        }
    }
}
