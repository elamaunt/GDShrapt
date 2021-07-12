namespace GDShrapt.Reader
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddSpace<T>(this T receiver, int count = 1)
            where T : ITokenReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.Space());
            return receiver;
        }

        public static T AddNewLine<T>(this T receiver)
            where T : INewLineReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.NewLine());
            return receiver;
        }

        public static T AddComment<T>(this T receiver, string comment)
            where T : ITokenReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.Comment(comment));
            return receiver;
        }

        public static T AddIntendation<T>(this T receiver, int count = 0)
            where T : IIntendedTokenReceiver
        {
            receiver.HandleReceivedToken(GD.Syntax.Intendation(count));
            return receiver;
        }

        public static T Add<T, B>(this T receiver, B token)
            where T : ITokenReceiver<B>
            where B : GDSyntaxToken
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T Add<T>(this T receiver, GDSingleOperatorType type)
           where T : ITokenReceiver<GDSingleOperator>
        {
            receiver.HandleReceivedToken(GD.Syntax.SingleOperator(type));
            return receiver;
        }

        public static T Add<T>(this T receiver, GDDualOperatorType type)
            where T : ITokenReceiver<GDDualOperator>
        {
            receiver.HandleReceivedToken(GD.Syntax.DualOperator(type));
            return receiver;
        }
        

        public static T Add<T>(this T receiver, long value)
            where T : ITokenReceiver<GDNumber>
        {
            receiver.HandleReceivedToken(GD.Syntax.Number(value));
            return receiver;
        }

        public static T Add<T>(this T receiver, int value)
           where T : ITokenReceiver<GDNumber>
        {
            receiver.HandleReceivedToken(GD.Syntax.Number(value));
            return receiver;
        }

        public static T Add<T>(this T receiver, double value)
            where T : ITokenReceiver<GDNumber>
        {
            receiver.HandleReceivedToken(GD.Syntax.Number(value));
            return receiver;
        }

        public static T Add<T>(this T receiver, string value, bool multiline = false, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas)
            where T : ITokenReceiver<GDString>
        {
            receiver.HandleReceivedToken(GD.Syntax.String(value, multiline, boundingChar));
            return receiver;
        }
    }
}
