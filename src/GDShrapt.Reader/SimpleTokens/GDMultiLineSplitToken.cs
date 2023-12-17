using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDMultiLineSplitToken : GDCharSequence
    {
        bool _facedNewLineToken;

        public override GDSyntaxToken Clone()
        {
            return new GDMultiLineSplitToken()
            {
                Sequence = Sequence
            };
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            base.HandleChar('\\', state);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            base.HandleChar(c, state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            base.HandleChar('\n', state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            base.HandleChar('#', state);
        }

        public override int NewLinesCount => Sequence?.Count(x => x == '\n') ?? 0;

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            if (_facedNewLineToken)
                return false;

            if (c.IsNewLine())
            {
                _facedNewLineToken = true;
                return true;
            }

            return true;
        }
    }
}
