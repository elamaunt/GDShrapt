namespace GDShrapt.Reader
{
    // Legacy
    /*internal class GDExportResolver : GDSequenceResolver
    {
        new ITokenOrSkipReceiver<GDExportDeclaration> Owner { get; }
        public override string Sequence => "export";

        public GDExportResolver(ITokenOrSkipReceiver<GDExportDeclaration> owner)
            : base(owner)
        {
            Owner = owner;
        }

        protected override void OnFail(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
        }

        protected override void OnMatch(GDReadingState state)
        {
            var declaration = new GDExportDeclaration();
            declaration.Add(new GDExportKeyword());
            Owner.HandleReceivedToken(declaration);
            state.Push(declaration);
        }
    }*/
}
