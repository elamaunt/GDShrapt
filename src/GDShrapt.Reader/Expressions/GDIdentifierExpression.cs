﻿namespace GDShrapt.Reader
{
    public class GDIdentifierExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Identifier);
        public GDIdentifier Identifier { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }

        public override string ToString()
        {
            return $"{Identifier}";
        }
    }
}