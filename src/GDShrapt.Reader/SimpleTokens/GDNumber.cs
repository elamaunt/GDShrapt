using System;
using System.Globalization;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDNumber : GDLiteralToken
    {
        readonly StringBuilder _stringBuilder = new StringBuilder();

        string _literalValue;

        GDNumberType _type;
        int _digitsCounter = 0;
        bool _justSwitchedToExponentialPart;
        bool _isExponentialPart;
        bool _exponentialPartSignNoticed;

        public double ValueDouble
        {
            get
            {
                switch (ResolveNumberType())
                {
                    case GDNumberType.Undefined:
                        throw new InvalidOperationException("The value is undefined");
                    case GDNumberType.LongDecimal:
                    case GDNumberType.LongBinary:
                    case GDNumberType.LongHexadecimal:
                        throw new InvalidOperationException("The value is in a Int64 format");
                    case GDNumberType.Double:
                        return double.Parse(PrepareString(_literalValue), CultureInfo.InvariantCulture);
                    default:
                        throw new InvalidOperationException("The value is undefined");
                }

            }
            set => _literalValue = Convert.ToString(value);
        }

        public long ValueInt64
        {
            get
            {
                switch (ResolveNumberType())
                {
                    case GDNumberType.Undefined:
                        throw new InvalidOperationException("The value is undefined");
                    case GDNumberType.LongDecimal:
                        return Convert.ToInt64(PrepareString(_literalValue), CultureInfo.InvariantCulture);
                    case GDNumberType.LongBinary:
                        return Convert.ToInt64(PrepareString(_literalValue).Substring(2), 2);
                    case GDNumberType.LongHexadecimal:
                        return long.Parse(PrepareString(_literalValue).Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    case GDNumberType.Double:
                        throw new InvalidOperationException("The value is in a Double format");
                    default:
                        throw new InvalidOperationException("The value is undefined");
                }

            }
            set => _literalValue = Convert.ToString(value);
        }

        public string ValueAsString
        {
            get => _literalValue;
            set
            {
                CheckIsValidString(value);
                _literalValue = value;
            }
        }

        public GDNumberType ResolveNumberType()
        {
            if (_literalValue.IsNullOrEmpty())
                return GDNumberType.Undefined;

            if (PrepareString(_literalValue).StartsWith("0x", StringComparison.Ordinal))
                return GDNumberType.LongHexadecimal;

            if (PrepareString(_literalValue).StartsWith("0b", StringComparison.Ordinal))
                return GDNumberType.LongBinary;

            if (_literalValue.IndexOfAny(new[] { '.', 'e' }) != -1)
                return GDNumberType.Double;

            return GDNumberType.LongDecimal;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (char.IsDigit(c))
            {
                _justSwitchedToExponentialPart = false;
                _digitsCounter++;

                if (_type == GDNumberType.Undefined)
                    _type = GDNumberType.LongDecimal;
                _stringBuilder.Append(c);
                return;
            }
            else
            {
                if (_stringBuilder.Length == 0 && c == '-')
                {
                    _stringBuilder.Append(c);
                    return;
                }

                if (_digitsCounter == 1 &&
                    (c == 'b' || c == 'x'))
                {
                    _type = c == 'b' ? GDNumberType.LongBinary : GDNumberType.LongHexadecimal;
                    _stringBuilder.Append(c);
                    return;
                }

                if (_stringBuilder.Length > 0)
                {
                    if (c == '.' && _type == GDNumberType.LongDecimal)
                    {
                        _type = GDNumberType.Double;
                        _stringBuilder.Append(c);
                        return;
                    }
                }

                if (_digitsCounter > 0)
                {
                    if (c == '_')
                    {
                        _stringBuilder.Append(c);
                        return;
                    }

                    if ((c == '-' || c == '+') && _isExponentialPart && !_exponentialPartSignNoticed && _justSwitchedToExponentialPart)
                    {
                        _exponentialPartSignNoticed = true;
                        _stringBuilder.Append(c);
                        return;
                    }

                    if ((c == 'e' || c == 'E') && _type == GDNumberType.Double && !_isExponentialPart)
                    {
                        _justSwitchedToExponentialPart = true;
                        _isExponentialPart = true;
                        _stringBuilder.Append(c);
                        return;
                    }

                    if (_type == GDNumberType.LongHexadecimal && (c == 'a' || c == 'A' ||
                        c == 'b' || c == 'B' ||
                        c == 'c' || c == 'C' ||
                        c == 'd' || c == 'D' ||
                        c == 'e' || c == 'E' ||
                        c == 'f' || c == 'F'))
                    {
                        _stringBuilder.Append(c);
                        return;
                    }
                }

                CompleteString();
                state.PopAndPass(c);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            CompleteString();
            state.PopAndPassNewLine();
        }

        internal override void ForceComplete(GDReadingState state)
        {
            CompleteString();
            base.ForceComplete(state);
        }

        private void CompleteString()
        {
            _literalValue = _stringBuilder.ToString();
            _stringBuilder.Clear();
        }

        private void CheckIsValidString(string value)
        {
            if (value.IsNullOrEmpty())
                throw new ArgumentException("Invalid number format");

            value = PrepareString(value);

            if (!TryReadBinary(value) &&
                !TryReadLong(value) &&
                !TryReadDouble(value))
                throw new ArgumentException("Invalid number format");
        }

        private bool TryReadDouble(string value)
        {
            return double.TryParse(value, NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out double d);
        }

        private bool TryReadLong(string value)
        {
            if (!value.StartsWith("0x", StringComparison.Ordinal))
            {
                return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v);
            }
            else
            {
                return long.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long v);
            }
        }

        private bool TryReadBinary(string value)
        {
            try
            {
                if (value.StartsWith("0b", StringComparison.Ordinal))
                    Convert.ToInt64(value.Substring(2), 2);
                else
                    return false;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Negates the literal value 
        /// </summary>
        public void Negate()
        {
            switch (IsNegative)
            {
                case true:
                    _literalValue = _literalValue.Substring(1);
                    break;
                case false:
                    _literalValue = "-" + _literalValue;
                    break;
                default:
                    break;
            }
        }

        public bool IsDefined => !_literalValue.IsNullOrEmpty();

        public bool? IsNegative
        {
            get
            {
                if (!IsDefined)
                    return null;

                return _literalValue[0] == '-';
            }
        }

        private static string PrepareString(string value)
        {
            return value.Replace("_", "");
        }

        public override GDSyntaxToken Clone()
        {
            return new GDNumber()
            {
                _literalValue = _literalValue
            };
        }

        public override GDDataToken CloneWith(string stringValue)
        {
            return new GDNumber()
            {
                ValueAsString = stringValue
            };
        }

        public override string StringDataRepresentation => _literalValue;

        public override string ToString()
        {
            return $"{_literalValue}";
        }
    }
}