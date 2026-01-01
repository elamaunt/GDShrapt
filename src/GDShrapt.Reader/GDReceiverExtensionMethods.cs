namespace GDShrapt.Reader
{
    /// <summary>
    /// Extension methods for token receivers used internally by the parser.
    /// These are minimal methods required for parsing operations.
    /// For full building API, use GDShrapt.Builder package.
    /// </summary>
    public static class GDReceiverExtensionMethods
    {
        public static T Add<T, B>(this T receiver, B token)
            where T : ITokenReceiver<B>
            where B : GDSyntaxToken
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T AddNewLine<T>(this T receiver)
            where T : INewLineReceiver
        {
            receiver.HandleReceivedToken(new GDNewLine());
            return receiver;
        }

        public static T AddSpace<T>(this T receiver, int count = 1)
            where T : ITokenReceiver
        {
            receiver.HandleReceivedToken(new GDSpace() { Sequence = new string(' ', count) });
            return receiver;
        }

        public static T AddAt<T>(this T receiver)
            where T : ITokenReceiver<GDAt>
        {
            receiver.HandleReceivedToken(new GDAt());
            return receiver;
        }
    }
}
