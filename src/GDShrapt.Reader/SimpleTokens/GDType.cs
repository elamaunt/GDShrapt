using System;

namespace GDShrapt.Reader
{
    public class GDType : GDCharSequence
    {
        public bool ExtractTypeFromInitializer => IsEmpty;
        public bool IsEmpty => Sequence.IsNullOrEmpty();
        public bool IsVoid => string.Equals(Sequence, "void", StringComparison.Ordinal);
        public bool IsObject => string.Equals(Sequence, "Object", StringComparison.Ordinal);
        public bool IsArray => string.Equals(Sequence, "Array", StringComparison.Ordinal);
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

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            if (SequenceBuilderLength == 0)
                return c == '_' || char.IsLetter(c);
            return c == '_' || char.IsLetterOrDigit(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}