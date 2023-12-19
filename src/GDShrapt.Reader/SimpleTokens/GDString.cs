using System;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDString : GDLiteralToken
    {
        readonly StringBuilder _stringBuilder = new StringBuilder();
        bool _stringStarted;
        int _boundingCharsCounter;
        bool _escapeNextChar;
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

                    if (c == '\\')
                    {
                        if (!_escapeNextChar)
                        {
                            _escapeNextChar = true;
                            _stringStarted = true;

                            for (int i = 0; i < _boundingCharsCounter - 1; i++)
                                _stringBuilder.Append(bc);

                            _boundingCharsCounter = 0;
                            _stringBuilder.Append(c);
                        }
                        else
                        {
                            _escapeNextChar = false;
                            _stringBuilder.Append(c);
                        }
                    }
                    else
                    {
                        if (c != bc)
                        {
                            _stringStarted = true;

                            for (int i = 0; i < _boundingCharsCounter - 1; i++)
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
                }

                return;
            }

            var boundingChar = GetBoundingChar();

            if (c == '\\')
            {
                if (!_escapeNextChar)
                {
                    _escapeNextChar = true;

                    for (int i = 0; i < _boundingCharsCounter; i++)
                        _stringBuilder.Append(boundingChar);

                    _boundingCharsCounter = 0;

                    _stringBuilder.Append(c);
                }
                else
                {
                    _escapeNextChar = false;
                    _stringBuilder.Append(c);
                }
            }
            else
            {
                if (c == boundingChar && !_escapeNextChar)
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
                    _escapeNextChar = false;
                    _stringBuilder.Append(c);
                }
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (Multiline)
            {
                HandleChar('\n', state);
            }
            else
            {
                Value = _stringBuilder.ToString();
                state.Pop();
                state.PassNewLine();
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

        public static implicit operator GDString(string value)
        {
            return new GDString()
            {
                Value = value
            };
        }

        public override GDDataToken CloneWith(string stringValue)
        {
            return new GDString()
            {
                Value = stringValue
            };
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            Value = _stringBuilder.ToString();
            base.ForceComplete(state);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDString()
            {
                Value = Value,
                BoundingChar = BoundingChar,
                Multiline = Multiline
            };
        }
        public override string StringDataRepresentation => Value;

        public override string ToString()
        {
            var c = GetBoundingChar();

            if (Multiline)
                return $"{c}{c}{c}{Value}{c}{c}{c}";

            return $"{c}{Value}{c}";
        }
    }
}