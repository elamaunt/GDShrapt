using System.Text;

namespace GDShrapt.Reader
{
    public abstract class GDSimpleSyntaxToken : GDSyntaxToken
    {
        public override void AppendTo(StringBuilder builder)
        {
            builder.Append(ToString());
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            state.PassSharpChar();
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            state.Pop();
            state.PassLeftSlashChar();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            state.Pop();
            state.PassCarriageReturnChar();
        }

        public override int Length => ToString().Length;
        public override int NewLinesCount => 0;
    }
}
