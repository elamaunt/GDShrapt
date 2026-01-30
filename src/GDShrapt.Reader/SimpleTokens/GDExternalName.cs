using System;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDExternalName : GDNameToken, IEquatable<GDExternalName>
    {
        string _sequence;
        public override string Sequence
        {
            get => _sequence;
            set
            {
                CheckNameValue(value);
                _sequence = value;
            }
        }

        private void CheckNameValue(string value)
        {
            if (value.IsNullOrWhiteSpace() || value.IsNullOrEmpty())
                throw new ArgumentException("Invalid name format");

            if (char.IsNumber(value[0]))
                throw new ArgumentException("Invalid name format");

            if (value.Any(x => !char.IsLetter(x) && !char.IsDigit(x) && x != '_'))
                throw new ArgumentException("Invalid name format");

        }

        StringBuilder _builder = new StringBuilder();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == '_' || char.IsLetterOrDigit(c))
            {
                _builder.Append(c);
            }
            else
            {
                Sequence = _builder.ToString();
                state.PopAndPass(c);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();

            state.PopAndPassNewLine();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();

            state.PopAndPassCarriageReturnChar();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();
            base.HandleSharpChar(state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();
            base.ForceComplete(state);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDExternalName()
            {
                Sequence = Sequence
            };
        }

        public static bool operator ==(GDExternalName one, GDExternalName two)
        {
            if (ReferenceEquals(one, null))
                return ReferenceEquals(two, null);

            if (ReferenceEquals(two, null))
                return false;

            return string.Equals(one.Sequence, two.Sequence, StringComparison.Ordinal);
        }

        public static bool operator !=(GDExternalName one, GDExternalName two)
        {
            if (ReferenceEquals(one, null))
                return !ReferenceEquals(two, null);

            if (ReferenceEquals(two, null))
                return true;

            return !string.Equals(one.Sequence, two.Sequence, StringComparison.Ordinal);
        }

        public static implicit operator GDExternalName(string id)
        {
            return new GDExternalName()
            {
                Sequence = id
            };
        }

        public override int GetHashCode()
        {
            return Sequence?.GetHashCode() ?? base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is GDExternalName identifier)
                return ReferenceEquals(Sequence, identifier.Sequence) || string.Equals(Sequence, identifier.Sequence, StringComparison.Ordinal);
            return base.Equals(obj);
        }

        public bool Equals(GDExternalName other)
        {
            return string.Equals(Sequence, other.Sequence, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}