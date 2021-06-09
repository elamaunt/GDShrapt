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

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            state.PassChar('#');
        }
    }
}
