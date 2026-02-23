using System.Text;

namespace GDShrapt.Reader
{
    internal class GDStringPartResolver : GDResolver
    {
        readonly GDStringBoundingChar _bounder;
        bool _triple;
        readonly bool _isRawString;

        int _boundingCharsCounter;
        bool _escapeNextChar;

        new ITokenOrSkipReceiver<GDStringPart> Owner { get; }

        readonly StringBuilder _stringBuilder = new StringBuilder();

        public GDStringPartResolver(ITokenOrSkipReceiver<GDStringPart> owner, GDStringBoundingChar bounder, bool isRawString = false)
            : base(owner)
        {
            Owner = owner;
            _bounder = bounder;
            _isRawString = isRawString;
            _triple = bounder == GDStringBoundingChar.TripleSingleQuotas || bounder == GDStringBoundingChar.TripleDoubleQuotas;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_escapeNextChar)
            {
                // https://docs.godotengine.org/en/stable/classes/class_string.html

                switch (c)
                {
                    case '\'': _stringBuilder.Append('\\').Append('\''); break;
                    case '"': _stringBuilder.Append('\\').Append('"'); break;
                    case '\\': _stringBuilder.Append('\\').Append('\\'); break;
                    case 'a': _stringBuilder.Append('\\').Append('a'); break;
                    case 'b': _stringBuilder.Append('\\').Append('b'); break;
                    case 'f': _stringBuilder.Append('\\').Append('f'); break;
                    case 'n': _stringBuilder.Append('\\').Append('n'); break;
                    case 'r': _stringBuilder.Append('\\').Append('r'); break;
                    case 't': _stringBuilder.Append('\\').Append('t'); break;
                    case 'v': _stringBuilder.Append('\\').Append('v'); break;
                    case 'u': _stringBuilder.Append('\\').Append('u'); break;
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
                return;
            }

            if (_triple)
            {
                if ((c == '\'' && _bounder == GDStringBoundingChar.TripleSingleQuotas) ||
                    (c == '"' && _bounder == GDStringBoundingChar.TripleDoubleQuotas))
                {
                    _boundingCharsCounter++;

                    if (_boundingCharsCounter == 3)
                    {
                        if (_stringBuilder.Length == 0)
                            Owner.HandleReceivedTokenSkip();
                        else
                            Owner.HandleReceivedToken(new GDStringPart() { Sequence = _stringBuilder.ToString() });

                        state.Pop();

                        if (_bounder == GDStringBoundingChar.TripleSingleQuotas)
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

                if (_bounder == GDStringBoundingChar.TripleSingleQuotas)
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
            if (_bounder == GDStringBoundingChar.TripleSingleQuotas || _bounder == GDStringBoundingChar.SingleQuotas)
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

            HandleChar('\n', state);
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (_bounder == GDStringBoundingChar.TripleSingleQuotas || _bounder == GDStringBoundingChar.SingleQuotas)
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

            HandleChar('\r', state);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            if (_isRawString)
            {
                _stringBuilder.Append('\\');
                return;
            }

            if (_bounder == GDStringBoundingChar.TripleSingleQuotas || _bounder == GDStringBoundingChar.SingleQuotas)
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

            if (_bounder == GDStringBoundingChar.TripleSingleQuotas || _bounder == GDStringBoundingChar.SingleQuotas)
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
