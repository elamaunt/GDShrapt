using System;

namespace GDShrapt.Reader
{
    internal static class GDResolvingHelper
    {
        public static void ResolveDualOperator(this ITokenOrSkipReceiver<GDDualOperator> receiver, char c, GDReadingState state)
        {
            state.PushAndPass(new GDDualOperatorResolver(receiver), c);
        }

        public static void ResolveSingleOperator(this ITokenOrSkipReceiver<GDSingleOperator> receiver, char c, GDReadingState state)
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

        public static void ResolveKeyword<T>(this ITokenOrSkipReceiver<T> receiver, char c, GDReadingState state)
            where T : GDSyntaxToken, IGDKeywordToken, new()
        {
            state.PushAndPass(new GDKeywordResolver<T>(receiver), c);
        }

        public static bool ResolveDollar(this ITokenOrSkipReceiver<GDDollar> receiver, char c, GDReadingState state)
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
        
        public static bool ResolveAt(this ITokenOrSkipReceiver<GDAt> receiver, char c, GDReadingState state)
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

        public static bool ResolveRightSlash(this ITokenOrSkipReceiver<GDRightSlash> receiver, char c, GDReadingState state)
        {
            var result = c == '/';
            if (result)
                receiver.HandleReceivedToken(new GDRightSlash());
            else
            {
                receiver.HandleReceivedTokenSkip();
                state.PassChar(c);
            }
            return result;
        }
        

        public static bool ResolveColon(this ITokenOrSkipReceiver<GDColon> receiver, char c, GDReadingState state)
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

        public static bool ResolveComma(this ITokenOrSkipReceiver<GDComma> receiver, char c, GDReadingState state)
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

        public static bool ResolveAssign(this ITokenOrSkipReceiver<GDAssign> receiver, char c, GDReadingState state)
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

        public static bool ResolveCloseBracket(this ITokenOrSkipReceiver<GDCloseBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveOpenBracket(this ITokenOrSkipReceiver<GDOpenBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveDefaultToken(this ITokenOrSkipReceiver<GDDefaultToken> receiver, char c, GDReadingState state)
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

        public static bool ResolveSquareOpenBracket(this ITokenOrSkipReceiver<GDSquareOpenBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveSquareCloseBracket(this ITokenOrSkipReceiver<GDSquareCloseBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveFigureOpenBracket(this ITokenOrSkipReceiver<GDFigureOpenBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveFigureCloseBracket(this ITokenOrSkipReceiver<GDFigureCloseBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveCornerOpenBracket(this ITokenOrSkipReceiver<GDCornerOpenBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveCornerCloseBracket(this ITokenOrSkipReceiver<GDCornerCloseBracket> receiver, char c, GDReadingState state)
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

        public static bool ResolveNewLine(this ITokenOrSkipReceiver<GDNewLine> receiver, char c, GDReadingState state)
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

        public static bool ResolveSemiColon(this ITokenOrSkipReceiver<GDSemiColon> receiver, char c, GDReadingState state)
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

        public static bool ResolvePoint(this ITokenOrSkipReceiver<GDPoint> receiver, char c, GDReadingState state)
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

        public static void ResolveExpression(this ITokenOrSkipReceiver<GDExpression> receiver, char c, GDReadingState state)
        {
            if (!IsExpressionStopChar(c))
                state.Push(new GDExpressionResolver(receiver));
            else
                receiver.HandleReceivedTokenSkip();
            state.PassChar(c);
        }

        public static bool ResolveDataToken(this ITokenOrSkipReceiver<GDDataToken> receiver, char c, GDReadingState state)
        {
            if (IsNumberStartChar(c) || c == '-')
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

        public static bool ResolveIdentifier(this ITokenOrSkipReceiver<GDIdentifier> receiver, char c, GDReadingState state)
        {
            if (IsIdentifierStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDIdentifier()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedTokenSkip();
            state.PassChar(c);
            return false;
        }

        public static bool ResolveType(this ITokenOrSkipReceiver<GDType> receiver, char c, GDReadingState state)
        {
            if (IsIdentifierStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDType()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedTokenSkip();
            state.PassChar(c);
            return false;
        }

        public static bool ResolveNumber(this ITokenOrSkipReceiver<GDNumber> receiver, char c, GDReadingState state)
        {
            if (IsNumberStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDNumber()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedTokenSkip();
            state.PassChar(c);
            return false;
        }

        public static bool ResolveSpaceToken(this ITokenReceiver<GDSpace> receiver, char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return true;
            }

            return false;
        }

        public static bool ResolveSpaceToken(this ITokenReceiver receiver, char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return true;
            }

            return false;
        }

        public static bool ResolveNewLineToken(this ITokenReceiver<GDNewLine> receiver, char c, GDReadingState state)
        {
            if (IsNewLine(c))
            {
                receiver.HandleReceivedToken(new GDNewLine());
                return true;
            }

            return false;
        }

        public static bool ResolveCommentToken(this ITokenReceiver<GDComment> receiver, char c, GDReadingState state)
        {
            if (IsCommentStartChar(c))
            {
                receiver.HandleReceivedToken(new GDComment());
                state.PassChar(c);
                return true;
            }

            return false;
        }

        public static bool ResolveString(this ITokenOrSkipReceiver<GDString> receiver, char c, GDReadingState state)
        {
            if (IsStringStartChar(c))
            {
                receiver.HandleReceivedToken(state.Push(new GDString()));
                state.PassChar(c);
                return true;
            }

            receiver.HandleReceivedTokenSkip();
            state.PassChar(c);
            return false;
        }

        public static bool IsDataStartCharToken(this char c) => IsIdentifierStartChar(c) || IsNumberStartChar(c) || IsStringStartChar(c) || IsNumberStartChar(c) || c == '-';
        public static bool IsCommentStartChar(this char c) => c == '#';
        public static bool IsSpace(this char c) => c == ' ' || c == '\t';
        public static bool IsIdentifierStartChar(this char c) => c == '_' || char.IsLetter(c);
        public static bool IsStringStartChar(this char c) => c == '\'' || c == '\"';
        public static bool IsExpressionStopChar(this char c) => c == ',' || c == '}' || c == ')' || c == ']' || c == ':' || c == ';';
        public static bool IsNumberStartChar(this char c) => char.IsDigit(c);
        public static bool IsNewLine(this char c) => c == '\n';
    }
}
