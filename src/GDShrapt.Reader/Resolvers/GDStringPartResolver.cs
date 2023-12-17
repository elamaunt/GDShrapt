using System;

namespace GDShrapt.Reader
{
    internal class GDStringPartResolver : GDResolver
    {
        new ITokenOrSkipReceiver<GDStringPart> Owner { get; }

        public GDStringPartResolver(ITokenOrSkipReceiver<GDStringPart> owner) 
            : base(owner)
        {
            Owner = owner;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new NotImplementedException();
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            base.HandleLeftSlashChar(state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            base.HandleSharpChar(state);
        }

    }
}
