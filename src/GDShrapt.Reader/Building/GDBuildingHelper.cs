namespace GDShrapt.Reader
{
    public static class GDBuildingHelper
    {
        public static T SendExpression<T>(this T receiver, GDExpression token)
           where T : ITokenReceiver<GDExpression>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendStatement<T>(this T receiver, GDStatement token)
            where T : ITokenReceiver<GDStatement>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendSpace<T>(this T receiver, GDSpace token)
            where T : ITokenReceiver<GDSpace>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendNewLine<T>(this T receiver, GDNewLine token)
            where T : ITokenReceiver<GDNewLine>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendComment<T>(this T receiver, GDComment token)
            where T : ITokenReceiver<GDComment>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendIntendation<T>(this T receiver, GDIntendation token)
            where T : ITokenReceiver<GDIntendation>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static B SendKeyword<T, B>(this B receiver, T keyword)
            where T : GDSyntaxToken, IGDKeywordToken, new()
            where B : ITokenReceiver<T>
        {
            receiver.HandleReceivedToken(keyword);
            return receiver;
        }

        public static B SendToken<T, B>(this B receiver, T token)
            where T : GDSyntaxToken, new()
            where B : ITokenReceiver<T>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendSingleOperator<T>(this T receiver, GDSingleOperator token)
            where T : ITokenReceiver<GDSingleOperator>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }

        public static T SendDualOperator<T>(this T receiver, GDDualOperator token)
            where T : ITokenReceiver<GDDualOperator>
        {
            receiver.HandleReceivedToken(token);
            return receiver;
        }




        /*public static NODE UnsafeConstruct<NODE>(params GDSyntaxToken[] unsafeTokens)
            where NODE : GDNode, new()
        {
            var node = new NODE();
            node.Form.SetTokens(unsafeTokens);
            return node;
        }

        /*public static NODE Construct<NODE>(params GDSyntaxToken[] unsafeTokens)
            where NODE : GDNode, new()
        {
            var node = new NODE();

            for (int i = 0; i < unsafeTokens.Length; i++)
            {
                var t = unsafeTokens[i];
                if (CheckStyleToken(node, t))
                    continue;

                if (CheckIntendationToken(node, t))
                    continue;

                if (CheckExpressionToken(node, t))
                    continue;

                if (CheckStatementToken(node, t))
                    continue;

                throw new GDInvalidStateException($"Unable to add token '{t.TypeName}' in the Node {typeof(NODE)}");
            }

            return node;
        }

        private static bool CheckStyleToken(IStyleTokensReceiver node, GDSyntaxToken token)
        {
            if (token is GDSpace space)
            {
                node.SendSpace(space);
                return true;
            }

            if (token is GDNewLine newLine)
            {
                node.SendNewLine(newLine);
                return true;
            }

            if (token is GDComment comment)
            {
                node.SendComment(comment);
                return true;
            }

            return false;
        }

        private static bool CheckIntendationToken(GDNode node, GDSyntaxToken token)
        {
            if (token is GDIntendation intendation)
            {
                ((IIntendationReceiver)node).SendIntendation(intendation);
                return true;
            }

            return false;
        }

        private static bool CheckExpressionToken(GDNode node, GDSyntaxToken token)
        {
            if (token is GDExpression expression)
            {
                ((IExpressionsReceiver)node).SendExpression(expression);
                return true;
            }

            return false;
        }

        private static bool CheckStatementToken(GDNode node, GDSyntaxToken token)
        {
            if (token is GDExpression expression)
            {
                ((IStatementsReceiver)node).SendStatement(expression);
                return true;
            }

            return false;
        }*/
    }
}
