using System;

namespace GDShrapt.Reader
{
    public sealed class GDGetNodeExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.GetNode);

        public string Path { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new NotImplementedException();
        }
    }
}
