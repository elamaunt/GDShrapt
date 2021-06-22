namespace GDShrapt.Reader
{
    internal class GDExportResolver : GDSequenceResolver
    {
        new IExportReceiver Owner { get; }
        public override string Sequence => "export";

        public GDExportResolver(IExportReceiver owner)
            : base(owner)
        {
            Owner = owner;
        }

        protected override void OnFail(GDReadingState state)
        {
            Owner.HandleReceivedExportSkip();
        }
        protected override void OnMatch(GDReadingState state)
        {
            var declaration = new GDExportDeclaration();
            declaration.SendKeyword(new GDExportKeyword());
            Owner.HandleReceivedExport(declaration);
            state.Push(declaration);
        }
    }
}
