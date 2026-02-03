namespace GDShrapt.Reader
{
    public class GDSingleTypeNode : GDTypeNode,
        ITokenOrSkipReceiver<GDType>
    {
        public override bool IsArray => Type?.IsArray ?? false;
        public override bool IsDictionary => Type?.IsDictionary ?? false;

        public GDType Type
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDType> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDType> TypedForm => _form;

        public GDSingleTypeNode()
        {
            _form = new GDTokensForm<State, GDType>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDSingleTypeNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Type)
            {
                if (!this.ResolveSpaceToken(c, state))
                    this.ResolveType(c, state);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.State = State.Completed;
            state.PopAndPassNewLine();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDType>.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                Type = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDType>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override string BuildName()
        {
            return $"{Type}";
        }

        public override bool IsNumericType() => Type?.IsInt == true || Type?.IsFloat == true;
        public override bool IsIntType() => Type?.IsInt == true;
        public override bool IsFloatType() => Type?.IsFloat == true;
        public override bool IsStringType() => Type?.Sequence == "String" || Type?.Sequence == "StringName";
        public override bool IsBoolType() => Type?.IsBool == true;
        public override bool IsVectorType()
        {
            var seq = Type?.Sequence;
            return seq == "Vector2" || seq == "Vector2i" ||
                   seq == "Vector3" || seq == "Vector3i" ||
                   seq == "Vector4" || seq == "Vector4i";
        }
        public override bool IsColorType() => Type?.IsColor == true;
    }
}
