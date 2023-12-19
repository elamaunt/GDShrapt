using System;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDType : GDNameToken
    {
        public bool ExtractTypeFromInitializer => IsEmpty;
        public bool IsEmpty => Sequence.IsNullOrEmpty();
        public bool IsBool => string.Equals(Sequence, "bool", StringComparison.Ordinal);
        public bool IsInt => string.Equals(Sequence, "int", StringComparison.Ordinal);
        public bool IsFloat => string.Equals(Sequence, "float", StringComparison.Ordinal);
        public bool IsVoid => string.Equals(Sequence, "void", StringComparison.Ordinal);
        public bool IsObject => string.Equals(Sequence, "Object", StringComparison.Ordinal);
        public bool IsArray => string.Equals(Sequence, "Array", StringComparison.Ordinal);
        public bool IsDictionary => string.Equals(Sequence, "Dictionary", StringComparison.Ordinal);
        public bool IsPoolByteArray => string.Equals(Sequence, "PoolByteArray", StringComparison.Ordinal);
        public bool IsPoolIntArray => string.Equals(Sequence, "PoolIntArray", StringComparison.Ordinal);
        public bool IsPoolRealArray => string.Equals(Sequence, "PoolRealArray", StringComparison.Ordinal);
        public bool IsPoolStringArray => string.Equals(Sequence, "PoolRealArray", StringComparison.Ordinal);
        public bool IsPoolVector2Array => string.Equals(Sequence, "PoolVector2Array", StringComparison.Ordinal);
        public bool IsPoolVector3Array => string.Equals(Sequence, "PoolVector3Array", StringComparison.Ordinal);
        public bool IsPoolColorArray => string.Equals(Sequence, "PoolColorArray", StringComparison.Ordinal);
        public bool IsRID => string.Equals(Sequence, "RID", StringComparison.Ordinal);
        public bool IsNodePath => string.Equals(Sequence, "NodePath", StringComparison.Ordinal);
        public bool IsColor => string.Equals(Sequence, "Color", StringComparison.Ordinal);
        public bool IsTransform => string.Equals(Sequence, "Transform", StringComparison.Ordinal);
        public bool IsBasis => string.Equals(Sequence, "Basis", StringComparison.Ordinal);
        public bool IsAABB => string.Equals(Sequence, "AABB", StringComparison.Ordinal);
        public bool IsQuat => string.Equals(Sequence, "Quat", StringComparison.Ordinal);
        public bool IsPlane => string.Equals(Sequence, "Plane", StringComparison.Ordinal);
        public bool IsTransform2D => string.Equals(Sequence, "Transform2D", StringComparison.Ordinal);
        public bool IsVector3 => string.Equals(Sequence, "Vector3", StringComparison.Ordinal);
        public bool IsRect2 => string.Equals(Sequence, "Rect2", StringComparison.Ordinal);
        public bool IsVector2 => string.Equals(Sequence, "Vector2", StringComparison.Ordinal);

        public string Sequence { get; set; }

        StringBuilder _builder = new StringBuilder();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_builder.Length == 0)
            {
                if (IsIdentifierStartChar(c))
                {
                    _builder.Append(c);
                }
                else
                {
                    state.PopAndPass(c);
                }
            }
            else
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

        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();
            state.PopAndPassNewLine();
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

        public static implicit operator GDType(string type)
        {
            return new GDType()
            {
                Sequence = type
            };
        }
        public override GDSyntaxToken Clone()
        {
            return new GDType()
            { 
                Sequence = Sequence
            };
        }

        public override GDDataToken CloneWith(string stringValue)
        {
            return new GDType()
            {
                Sequence = stringValue
            };
        }

        public override string StringDataRepresentation => Sequence;

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}