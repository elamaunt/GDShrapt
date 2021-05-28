namespace GDSharp.Reader
{
    public class GDTypeDeclarationResolver : GDNode
    {
        public GDReadingState State { get; }

        public GDTypeDeclarationResolver(GDReadingState state)
        {
            State = state;
        }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.PushNode(State.Type = new GDClassDeclaration());

            state.HandleChar(c);

            // TODO: Resolve another types. Currently class is the first goal.
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            // TODO: handle in future
        }
    }
}