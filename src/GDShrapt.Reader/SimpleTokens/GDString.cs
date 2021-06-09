using System;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDString : GDSimpleSyntaxToken
    {
        readonly StringBuilder _stringBuilder = new StringBuilder();
        bool _stringStarted;
        int _boundingCharsCounter;

        public bool Multiline { get; set; }
        public GDStringBoundingChar BoundingChar { get; set; }
        public string Value { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_stringStarted)
            {
                if (_boundingCharsCounter == 0)
                {
                    if (c == '"')
                    {
                        BoundingChar = GDStringBoundingChar.DoubleQuotas;
                        _boundingCharsCounter++;
                        return;
                    }

                    if (c == '\'')
                    {
                        BoundingChar = GDStringBoundingChar.SingleQuotas;
                        _boundingCharsCounter++;
                        return;
                    }
                }
                else
                {
                    var bc = GetBoundingChar();
                    if (c != bc)
                    {
                        _stringStarted = true;

                        for (int i = 0; i < _boundingCharsCounter -1; i++)
                            _stringBuilder.Append(bc);

                        _boundingCharsCounter = 0;
                        _stringBuilder.Append(c);
                    }
                    else
                    {
                        _boundingCharsCounter++;

                        if (_boundingCharsCounter == 3)
                        {
                            Multiline = true;
                            _boundingCharsCounter = 0;
                            _stringStarted = true;
                        }

                    }

                }

                return;
            }

            var boundingChar = GetBoundingChar();

            if (c == boundingChar)
            {
                if (!Multiline)
                {
                    Value = _stringBuilder.ToString();
                    state.Pop();
                }
                else
                {
                    _boundingCharsCounter++;

                    if (_boundingCharsCounter == 3)
                    {
                        Value = _stringBuilder.ToString();
                        state.Pop();
                    }
                }
            }
            else
            {
                for (int i = 0; i < _boundingCharsCounter; i++)
                    _stringBuilder.Append(boundingChar);

                _boundingCharsCounter = 0;
                _stringBuilder.Append(c);
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (Multiline)
            {
                HandleChar('\n', state);
            }
            else
            {
                Value = _stringBuilder.ToString();
                state.Pop();
                state.PassLineFinish();
            }
        }

        public char GetBoundingChar()
        {
            switch (BoundingChar)
            {
                case GDStringBoundingChar.SingleQuotas:
                    return '\'';
                case GDStringBoundingChar.DoubleQuotas:
                    return '"';
                default:
                    throw new NotSupportedException();
            }
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        public override string ToString()
        {
            if (Multiline)
                return $"\"\"\"{Value}\"\"\"";

            var c = GetBoundingChar();
            return $"{c}{Value}{c}";
        }
    }
}