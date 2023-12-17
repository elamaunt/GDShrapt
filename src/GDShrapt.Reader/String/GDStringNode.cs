namespace GDShrapt.Reader
{
    public class GDMultilineDoubleQuotasStringNode : GDStringNode<GDTripleDoubleQuotas>
    {
        public override GDNode CreateEmptyInstance()
        {
            return new GDMultilineDoubleQuotasStringNode();
        }
    }

    public class GDMultilineSingleQuotasStringNode : GDStringNode<GDTripleSingleQuotas>
    { 
        public override GDNode CreateEmptyInstance()
        {
            return new GDMultilineSingleQuotasStringNode();
        }
    }

    public class GDDoubleQuotasStringNode : GDStringNode<GDDoubleQuotas>
    {
        public override GDNode CreateEmptyInstance()
        {
            return new GDDoubleQuotasStringNode();
        }
    }

    public class GDSingleQuotasStringNode : GDStringNode<GDSingleQuotas>
    {
        public override GDNode CreateEmptyInstance()
        {
            return new GDSingleQuotasStringNode();
        }
    }

    public abstract class GDStringNode : GDNode
    {
        public abstract GDStringPartsList Parts { get; set; }
    }

    public abstract class GDStringNode<BOUNDER> : GDStringNode
        where BOUNDER : GDSyntaxToken
    {
        public BOUNDER OpeningBounder
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public override GDStringPartsList Parts
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public BOUNDER ClosingBounder
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public enum State 
        { 
            OpeningBounder,
            StringParts,
            ClosingBounder,
            Completed
        }

        readonly GDTokensForm<State, BOUNDER, GDStringPartsList, BOUNDER> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, BOUNDER, GDStringPartsList, BOUNDER> TypedForm => _form;

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void Left(IGDVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            throw new System.NotImplementedException();
        }
    }
}
