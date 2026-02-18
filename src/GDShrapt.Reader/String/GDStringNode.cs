using System.Linq;

namespace GDShrapt.Reader
{
    public class GDTripleDoubleQuotasStringNode : GDStringNode<GDTripleDoubleQuotas>
    {
        public override GDStringPartsList Parts
        {
            get => _form.GetOrInit(1, new GDStringPartsList(GDStringBoundingChar.TripleDoubleQuotas));
            set => _form.Token1 = value;
        }

        public override GDStringBoundingChar BoundingChar =>  GDStringBoundingChar.TripleDoubleQuotas;

        public override GDNode CreateEmptyInstance()
        {
            return new GDTripleDoubleQuotasStringNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.ClosingBounder:
                case State.OpeningBounder:
                    this.ResolveTripleDoubleQuotas(c, state);
                    break;
                case State.StringParts:
                    _form.State = State.ClosingBounder;
                    state.PushAndPass(Parts, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class GDTripleSingleQuotasStringNode : GDStringNode<GDTripleSingleQuotas>
    {
        public override GDStringPartsList Parts
        {
            get => _form.GetOrInit(1, new GDStringPartsList(GDStringBoundingChar.TripleSingleQuotas));
            set => _form.Token1 = value;
        }
        public override GDStringBoundingChar BoundingChar => GDStringBoundingChar.TripleSingleQuotas;

        public override GDNode CreateEmptyInstance()
        {
            return new GDTripleSingleQuotasStringNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.ClosingBounder:
                case State.OpeningBounder:
                    this.ResolveTripleSingleQuotas(c, state);
                    break;
                case State.StringParts:
                    _form.State = State.ClosingBounder;
                    state.PushAndPass(Parts, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class GDDoubleQuotasStringNode : GDStringNode<GDDoubleQuotas>
    {
        public override GDStringPartsList Parts
        {
            get => _form.GetOrInit(1, new GDStringPartsList(GDStringBoundingChar.DoubleQuotas));
            set => _form.Token1 = value;
        }
        public override GDStringBoundingChar BoundingChar => GDStringBoundingChar.DoubleQuotas;
        public override GDNode CreateEmptyInstance()
        {
            return new GDDoubleQuotasStringNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.ClosingBounder:
                case State.OpeningBounder:
                    this.ResolveDoubleQuotas(c, state);
                    break;
                case State.StringParts:
                    _form.State = State.ClosingBounder;
                    state.PushAndPass(Parts, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class GDSingleQuotasStringNode : GDStringNode<GDSingleQuotas>
    {
        public override GDStringPartsList Parts
        {
            get => _form.GetOrInit(1, new GDStringPartsList(GDStringBoundingChar.SingleQuotas));
            set => _form.Token1 = value;
        }
        public override GDStringBoundingChar BoundingChar => GDStringBoundingChar.SingleQuotas;

        public override GDNode CreateEmptyInstance()
        {
            return new GDSingleQuotasStringNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.ClosingBounder:
                case State.OpeningBounder:
                    this.ResolveSingleQuotas(c, state);
                    break;
                case State.StringParts:
                    _form.State = State.ClosingBounder;
                    state.PushAndPass(Parts, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public abstract class GDStringNode : GDNode, ITokenOrSkipReceiver<GDStringPartsList>
    {
        public abstract GDStringPartsList Parts { get; set; }
        public abstract GDStringBoundingChar BoundingChar { get; }
        public string Sequence => string.Concat(Parts.Select(x => x.Sequence ?? ""));
        public string EscapedSequence => string.Concat(Parts.Select(x => x.EscapedSequence ?? ""));

        public abstract void HandleReceivedToken(GDStringPartsList token);
        public abstract void HandleReceivedTokenSkip();
    }

    public abstract class GDStringNode<BOUNDER> : GDStringNode,
        ITokenOrSkipReceiver<BOUNDER>
      
        where BOUNDER : GDSimpleSyntaxToken
    {
        public BOUNDER OpeningBounder
        {
            get => _form.Token0;
            set => _form.Token0 = value;
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

        protected readonly GDTokensForm<State, BOUNDER, GDStringPartsList, BOUNDER> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, BOUNDER, GDStringPartsList, BOUNDER> TypedForm => _form;

        public GDStringNode()
        {
            _form = new GDTokensForm<State, BOUNDER, GDStringPartsList, BOUNDER>(this);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            HandleChar('\\', state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            HandleChar('\n', state);
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (_form.State == State.Completed)
            {
                state.PopAndPassCarriageReturnChar();
                return;
            }

            // For incomplete strings, CR is not valid - but pass through HandleChar for error handling
            HandleChar('\r', state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }

        void ITokenReceiver<BOUNDER>.HandleReceivedToken(BOUNDER token)
        {
            if (_form.IsOrLowerState(State.OpeningBounder))
            {
                OpeningBounder = token;
                _form.State = State.StringParts;
                return;
            }

            if (_form.IsOrLowerState(State.ClosingBounder))
            {
                ClosingBounder = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<BOUNDER>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.OpeningBounder))
            {
                _form.State = State.StringParts;
                return;
            }

            if (_form.IsOrLowerState(State.ClosingBounder))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override void HandleReceivedToken(GDStringPartsList token)
        {
            if (_form.IsOrLowerState(State.StringParts))
            {
                Parts = token;
                _form.State = State.ClosingBounder;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override void HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.StringParts))
            {
                _form.State = State.ClosingBounder;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
