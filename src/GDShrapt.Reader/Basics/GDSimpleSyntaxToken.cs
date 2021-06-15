using System.Text;

namespace GDShrapt.Reader
{
    public abstract class GDSimpleSyntaxToken : GDSyntaxToken
    {
        public override void AppendTo(StringBuilder builder)
        {
            builder.Append(ToString());
        }

        internal override void ForceComplete(GDReadingState state)
        {
            state.Pop();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            state.PassChar('#');
        }
    }
}
