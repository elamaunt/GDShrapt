using System;

namespace GDShrapt.Reader
{
    internal static class GDResolvingHelper
    {
        public static void SendExpression(this IExpressionsReceiver receiver, GDExpression token)
        {
            receiver.HandleReceivedToken(token);
        }

        public static void SendSpace(this IStyleTokensReceiver receiver, GDSpace token)
        {
            receiver.HandleReceivedToken(token);
        }

        public static void SendKeyword<T>(this IKeywordReceiver<T> receiver, T keyword)
            where T : GDSyntaxToken, IGDKeywordToken, new()
        {
            receiver.HandleReceivedToken(keyword);
        }

        public static void SendToken<T>(this ITokenReceiver<T> receiver, T token)
            where T : GDSyntaxToken, new()
        {
            receiver.HandleReceivedToken(token);
        }

        public static void SendSingleOperator(this ISingleOperatorReceiver receiver, GDSingleOperator token)
        {
            receiver.HandleReceivedToken(token);
        }

        public static void SendDualOperator(this IDualOperatorReceiver receiver, GDDualOperator token)
        {
            receiver.HandleReceivedToken(token);
        }

        public static void ResolveDualOperator(this IDualOperatorReceiver receiver, char c, GDReadingState state)
        {
            state.PushAndPass(new GDDualOperatorResolver(receiver), c);
        }

        public static void ResolveSingleOperator(this ISingleOperatorReceiver receiver, char c, GDReadingState state)
        {
            state.PushAndPass(new GDSingleOperatorResolver(receiver), c);
        }

        public static bool ResolveInvalidToken(this ITokenReceiver receiver, char c, GDReadingState state, Predicate<char> test)
        {
            if (test(c))
                return false;

            receiver.HandleReceivedToken(state.Push(new GDInvalidToken()));
            state.PassChar(c);
            return true;
        }

        public static bool ResolvePath(this IPathReceiver receiver, char c, GDReadingState state)
        {
            if (!IsSpace(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDPath()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedIdentifierSkip();
            state.PassChar(c);
            return false;
        }

        public static void ResolveKeyword<T>(this IKeywordReceiver<T> receiver, char c, GDReadingState state)
            where T : GDSyntaxToken, IGDKeywordToken, new()
        {
            state.PushAndPass(new GDKeywordResolver<T>(receiver), c);
        }

        public static bool ResolveDollar(this ITokenReceiver<GDDollar> receiver, char c, GDReadingState state)
        {
            var result = c == '$';
            if (result)
                receiver.HandleReceivedToken(new GDDollar());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }
        
        public static bool ResolveAt(this ITokenReceiver<GDAt> receiver, char c, GDReadingState state)
        {
            var result = c == '@';
            if (result)
                receiver.HandleReceivedToken(new GDAt());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveColon(this ITokenReceiver<GDColon> receiver, char c, GDReadingState state)
        {
            var result = c == ':';
            if (result)
                receiver.HandleReceivedToken(new GDColon());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveComma(this ITokenReceiver<GDComma> receiver, char c, GDReadingState state)
        {
            var result = c == ',';
            if (result)
                receiver.HandleReceivedToken(new GDComma());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }

            return result;
        }

        public static bool ResolveAssign(this ITokenReceiver<GDAssign> receiver, char c, GDReadingState state)
        {
            var result = c == '=';
            if (result)
                receiver.HandleReceivedToken(new GDAssign());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveCloseBracket(this ITokenReceiver<GDCloseBracket> receiver, char c, GDReadingState state)
        {
            var result = c == ')';
            if (result)
                receiver.HandleReceivedToken(new GDCloseBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveOpenBracket(this ITokenReceiver<GDOpenBracket> receiver, char c, GDReadingState state)
        {
            var result = c == '(';
            if (result)
                receiver.HandleReceivedToken(new GDOpenBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveDefaultToken(this ITokenReceiver<GDDefaultToken> receiver, char c, GDReadingState state)
        {
            var result = c == '_';
            if (result)
                receiver.HandleReceivedToken(new GDDefaultToken());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveSquareOpenBracket(this ITokenReceiver<GDSquareOpenBracket> receiver, char c, GDReadingState state)
        {
            var result = c == '[';
            if (result)
                receiver.HandleReceivedToken(new GDSquareOpenBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveSquareCloseBracket(this ITokenReceiver<GDSquareCloseBracket> receiver, char c, GDReadingState state)
        {
            var result = c == ']';
            if (result)
                receiver.HandleReceivedToken(new GDSquareCloseBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveFigureOpenBracket(this ITokenReceiver<GDFigureOpenBracket> receiver, char c, GDReadingState state)
        {
            var result = c == '{';
            if (result)
                receiver.HandleReceivedToken(new GDFigureOpenBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveFigureCloseBracket(this ITokenReceiver<GDFigureCloseBracket> receiver, char c, GDReadingState state)
        {
            var result = c == '}';
            if (result)
                receiver.HandleReceivedToken(new GDFigureCloseBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveCornerOpenBracket(this ITokenReceiver<GDCornerOpenBracket> receiver, char c, GDReadingState state)
        {
            var result = c == '<';
            if (result)
                receiver.HandleReceivedToken(new GDCornerOpenBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveCornerCloseBracket(this ITokenReceiver<GDCornerCloseBracket> receiver, char c, GDReadingState state)
        {
            var result = c == '>';
            if (result)
                receiver.HandleReceivedToken(new GDCornerCloseBracket());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveNewLine(this ITokenReceiver<GDNewLine> receiver, char c, GDReadingState state)
        {
            var result = c == '\n';
            if (result)
                receiver.HandleReceivedToken(new GDNewLine());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolveSemiColon(this ITokenReceiver<GDSemiColon> receiver, char c, GDReadingState state)
        {
            var result = c == ';';
            if (result)
                receiver.HandleReceivedToken(new GDSemiColon());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static bool ResolvePoint(this ITokenReceiver<GDPoint> receiver, char c, GDReadingState state)
        {
            var result = c == '.';
            if (result)
                receiver.HandleReceivedToken(new GDPoint());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }

        public static void ResolveExpression(this IExpressionsReceiver receiver, char c, GDReadingState state)
        {
            if (!IsExpressionStopChar(c))
                state.Push(new GDExpressionResolver(receiver));
            else
                receiver.HandleReceivedExpressionSkip();
            state.PassChar(c);
        }

        public static bool ResolveDataToken(this IDataTokenReceiver receiver, char c, GDReadingState state)
        {
            if (IsNumberStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDNumber()));
                state.PassChar(c);
                return true;
            }

            if (IsStringStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDString()));
                state.PassChar(c);
                return true;
            }

            if (IsIdentifierStartChar(c))
            {
                state.Push(new GDIdentifier());
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedTokenSkip();
            return false;
        }

        public static bool ResolveIdentifier(this IIdentifierReceiver receiver, char c, GDReadingState state)
        {
            if (IsIdentifierStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDIdentifier()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedIdentifierSkip();
            state.PassChar(c);
            return false;
        }

        public static bool ResolveType(this ITypeReceiver receiver, char c, GDReadingState state)
        {
            if (IsIdentifierStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDType()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedTypeSkip();
            state.PassChar(c);
            return false;
        }

        public static bool ResolveNumber(this INumberReceiver receiver, char c, GDReadingState state)
        {
            if (IsNumberStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDNumber()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedNumberSkip();
            state.PassChar(c);
            return false;
        }

        public static bool ResolveStyleToken(this IStyleTokensReceiver receiver, char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return true;
            }

            if (IsNewLine(c))
            {
                receiver.HandleReceivedToken(new GDNewLine());
                return true;
            }

            if (IsCommentStartChar(c))
            {
                receiver.HandleReceivedToken(new GDComment());
                state.PassChar(c);
                return true;
            }

            return false;
        }

        public static bool ResolveString(this IStringReceiver receiver, char c, GDReadingState state)
        {
            if (IsStringStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDString()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedStringSkip();
            state.PassChar(c);
            return false;
        }

        public static bool IsDataStartCharToken(this char c) => IsIdentifierStartChar(c) || IsNumberStartChar(c) || IsStringStartChar(c);
        public static bool IsCommentStartChar(this char c) => c == '#';
        public static bool IsSpace(this char c) => c == ' ' || c == '\t';
        public static bool IsIdentifierStartChar(this char c) => c == '_' || char.IsLetter(c);
        public static bool IsStringStartChar(this char c) => c == '\'' || c == '\"';
        public static bool IsExpressionStopChar(this char c) => c == ',' || c == '}' || c == ')' || c == ']' || c == ':' || c == ';';
        public static bool IsNumberStartChar(this char c) => char.IsDigit(c);
        public static bool IsNewLine(this char c) => c == '\n';
    }
}
