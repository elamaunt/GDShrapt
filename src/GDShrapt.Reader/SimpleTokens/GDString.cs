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

        string _sequence;
        public override string Sequence 
        {
            get
            {
                if (_sequence == null)
                    return null;

                if (_sequence.Length == 0)
                    return _sequence;

                switch (BoundingChar)
                {
                    case GDStringBoundingChar.SingleQuotas:
                        {
                            var builder = new StringBuilder();

                            for (int i = 0; i < _sequence.Length; i++)
                            {
                                var ch = _sequence[i];

                                if (ch == '\\' || ch == '\'')
                                    builder.Append('\\');
                                builder.Append(ch);
                            }

                            return builder.ToString();
                        }
                    case GDStringBoundingChar.DoubleQuotas:
                        {
                            var builder = new StringBuilder();

                            for (int i = 0; i < _sequence.Length; i++)
                            {
                                var ch = _sequence[i];

                                if (ch == '\\' || ch == '"')
                                    builder.Append('\\');
                                builder.Append(ch);
                            }

                            return builder.ToString();
                        }
                    default:
                        return _sequence;
                }
            }
            set
            {
                _sequence = value;
            }
        }

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
                        Sequence = _stringBuilder.ToString();
                        state.Pop();
                    }
                    else
                    {
                        _boundingCharsCounter++;

                        if (_boundingCharsCounter == 3)
                        {
                            Sequence = _stringBuilder.ToString();
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
                Sequence = _stringBuilder.ToString();
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
                Sequence = value
            };
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            Sequence = _stringBuilder.ToString();
            base.ForceComplete(state);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDString()
            {
                Sequence = Sequence,
                BoundingChar = BoundingChar,
                Multiline = Multiline
            };
        }

        public override string ToString()
        {
            var c = GetBoundingChar();

            if (Multiline)
                return $"{c}{c}{c}{Sequence}{c}{c}{c}";

            return $"{c}{Sequence}{c}";
        }
    }
}