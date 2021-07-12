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
            receiver.HandleReceivedToken(GD.Syntax.NewLine);
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

        public static T Add<T>(this T receiver, string identifier)
            where T : ITokenReceiver<GDIdentifier>
        {
            receiver.HandleReceivedToken(GD.Syntax.Identifier(identifier));
            return receiver;
        }

        public static T AddType<T>(this T receiver, string type)
            where T : ITokenReceiver<GDType>
        {
            receiver.HandleReceivedToken(GD.Syntax.Type(type));
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

        public static T AddNumber<T>(this T receiver, string value)
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

        public static T AddFigureOpenBracket<T>(this T receiver)
            where T : ITokenReceiver<GDFigureOpenBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.FigureOpenBracket);
            return receiver;
        }

        public static T AddFigureCloseBracket<T>(this T receiver)
            where T : ITokenReceiver<GDFigureCloseBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.FigureCloseBracket);
            return receiver;
        }

        public static T AddSquareOpenBracket<T>(this T receiver)
            where T : ITokenReceiver<GDSquareOpenBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.SquareOpenBracket);
            return receiver;
        }

        public static T AddSquareCloseBracket<T>(this T receiver)
            where T : ITokenReceiver<GDSquareCloseBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.SquareCloseBracket);
            return receiver;
        }

        public static T AddCornerOpenBracket<T>(this T receiver)
            where T : ITokenReceiver<GDCornerOpenBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.CornerOpenBracket);
            return receiver;
        }

        public static T AddCornerCloseBracket<T>(this T receiver)
            where T : ITokenReceiver<GDCornerCloseBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.CornerCloseBracket);
            return receiver;
        }

        public static T AddOpenBracket<T>(this T receiver)
            where T : ITokenReceiver<GDOpenBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.OpenBracket);
            return receiver;
        }

        public static T AddCloseBracket<T>(this T receiver)
            where T : ITokenReceiver<GDCloseBracket>
        {
            receiver.HandleReceivedToken(GD.Syntax.CloseBracket);
            return receiver;
        }

        public static T AddDefaultToken<T>(this T receiver)
            where T : ITokenReceiver<GDDefaultToken>
        {
            receiver.HandleReceivedToken(GD.Syntax.DefaultToken);
            return receiver;
        }

        public static T AddRightSlash<T>(this T receiver)
            where T : ITokenReceiver<GDRightSlash>
        {
            receiver.HandleReceivedToken(GD.Syntax.RightSlash);
            return receiver;
        }

        public static T AddPoint<T>(this T receiver)
            where T : ITokenReceiver<GDPoint>
        {
            receiver.HandleReceivedToken(GD.Syntax.Point);
            return receiver;
        }

        public static T AddDollar<T>(this T receiver)
            where T : ITokenReceiver<GDDollar>
        {
            receiver.HandleReceivedToken(GD.Syntax.Dollar);
            return receiver;
        }

        public static T AddComma<T>(this T receiver)
            where T : ITokenReceiver<GDComma>
        {
            receiver.HandleReceivedToken(GD.Syntax.Comma);
            return receiver;
        }

        public static T AddAt<T>(this T receiver)
            where T : ITokenReceiver<GDAt>
        {
            receiver.HandleReceivedToken(GD.Syntax.At);
            return receiver;
        }

        public static T AddAssign<T>(this T receiver)
            where T : ITokenReceiver<GDAssign>
        {
            receiver.HandleReceivedToken(GD.Syntax.Assign);
            return receiver;
        }

        public static T AddColon<T>(this T receiver)
           where T : ITokenReceiver<GDColon>
        {
            receiver.HandleReceivedToken(GD.Syntax.Colon);
            return receiver;
        }

        public static T AddSemiColon<T>(this T receiver)
          where T : ITokenReceiver<GDSemiColon>
        {
            receiver.HandleReceivedToken(GD.Syntax.SemiColon);
            return receiver;
        }
    }
}
