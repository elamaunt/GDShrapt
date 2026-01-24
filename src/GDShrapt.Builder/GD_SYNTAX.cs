using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public static partial class GD
    {
        private static readonly GDScriptReader _reader = new GDScriptReader();

        internal static GDTypeNode ParseTypeNode(string type) => _reader.ParseType(type);

        public static class Syntax
        {
            public static GDIdentifier Identifier(string name) => new GDIdentifier() { Sequence = name ?? throw new ArgumentNullException(nameof(name)) };
            public static GDType Type(string name) => new GDType() { Sequence = name ?? throw new ArgumentNullException(nameof(name)) };

            public static GDStringNode String(string value, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                switch (boundingChar)
                {
                    case GDStringBoundingChar.SingleQuotas:
                        return new GDSingleQuotasStringNode()
                        { 
                            OpeningBounder = new GDSingleQuotas(),
                            Parts = new GDStringPartsList() { new GDStringPart() { Sequence = value } },
                            ClosingBounder = new GDSingleQuotas()
                        };
                    case GDStringBoundingChar.DoubleQuotas:
                        return new GDDoubleQuotasStringNode()
                        {
                            OpeningBounder = new GDDoubleQuotas(),
                            Parts = new GDStringPartsList() { new GDStringPart() { Sequence = value } },
                            ClosingBounder = new GDDoubleQuotas()
                        };
                    case GDStringBoundingChar.TripleDoubleQuotas:
                        return new GDTripleDoubleQuotasStringNode() 
                        { 
                            OpeningBounder = new GDTripleDoubleQuotas(),
                            Parts = new GDStringPartsList() { new GDStringPart() { Sequence = value } },
                            ClosingBounder = new GDTripleDoubleQuotas()
                        };
                    case GDStringBoundingChar.TripleSingleQuotas:
                        return new GDTripleSingleQuotasStringNode()
                        { 
                            OpeningBounder = new GDTripleSingleQuotas(),
                            Parts = new GDStringPartsList() { new GDStringPart() { Sequence = value } },
                            ClosingBounder = new GDTripleSingleQuotas()
                        };
                    default:
                        throw new ArgumentOutOfRangeException(nameof(boundingChar), boundingChar, "Unsupported string bounding character type");
                }
            }

            public static GDNumber Number(string stringValue) => new GDNumber() { Sequence = stringValue ?? throw new ArgumentNullException(nameof(stringValue)) };
            public static GDNumber Number(int value) => new GDNumber() { ValueInt64 = value };
            public static GDNumber Number(long value) => new GDNumber() { ValueInt64 = value };
            public static GDNumber Number(double value) => new GDNumber() { ValueDouble = value };

            public static GDSpace OneSpace => Space();
            public static GDSpace Space(int count = 1) => new GDSpace() { Sequence = new string(' ', count) };
            public static GDSpace Space(string whiteSpace) => new GDSpace() { Sequence = whiteSpace };
            public static GDNewLine NewLine => new GDNewLine();
            public static GDLeftSlash LeftSlash => new GDLeftSlash();
            public static GDIntendation Intendation(int count = 0) => new GDIntendation()
            { 
                Sequence = new string('\t', count),
                LineIntendationThreshold = count 
            };
            public static GDComment Comment(string comment) => new GDComment() { Sequence = comment ?? throw new ArgumentNullException(nameof(comment)) };

            public static GDFigureOpenBracket FigureOpenBracket => new GDFigureOpenBracket();
            public static GDFigureCloseBracket FigureCloseBracket => new GDFigureCloseBracket();
            public static GDSquareOpenBracket SquareOpenBracket => new GDSquareOpenBracket();
            public static GDSquareCloseBracket SquareCloseBracket => new GDSquareCloseBracket();
            public static GDCornerOpenBracket CornerOpenBracket => new GDCornerOpenBracket();
            public static GDCornerCloseBracket CornerCloseBracket => new GDCornerCloseBracket();
            public static GDOpenBracket OpenBracket => new GDOpenBracket();
            public static GDCloseBracket CloseBracket => new GDCloseBracket();
            public static GDDefaultToken DefaultToken => new GDDefaultToken();
            public static GDRightSlash RightSlash => new GDRightSlash();
            public static GDPoint Point => new GDPoint();
            public static GDDollar Dollar => new GDDollar();
            public static GDComma Comma => new GDComma();
            public static GDAt At => new GDAt();
            public static GDAssign Assign => new GDAssign();
            public static GDColon Colon => new GDColon();
            public static GDSemiColon SemiColon => new GDSemiColon();

            public static GDPathSpecifier PathSpecifier(GDPathSpecifierType specifier) => new GDPathSpecifier() { Type = specifier };
            public static GDPathSpecifier PathSpecifier(string identifier) => new GDPathSpecifier() { Type = GDPathSpecifierType.Identifier, IdentifierValue = identifier ?? throw new ArgumentNullException(nameof(identifier)) };

            public static GDDualOperator DualOperator(GDDualOperatorType type) => new GDDualOperator()
            {
                OperatorType = type
            };

            public static GDSingleOperator SingleOperator(GDSingleOperatorType type) => new GDSingleOperator()
            {
                OperatorType = type
            };

            /// <summary>
            /// Creates a line continuation token (backslash followed by newline)
            /// </summary>
            public static GDMultiLineSplitToken LineContinuation() => new GDMultiLineSplitToken() { Sequence = "\\\n" };

            /// <summary>
            /// Creates a line continuation with optional trailing content (e.g., spaces before newline)
            /// </summary>
            public static GDMultiLineSplitToken LineContinuation(string trailing) => new GDMultiLineSplitToken()
            {
                Sequence = "\\" + trailing + "\n"
            };

            /// <summary>
            /// Creates a multiline string using triple double quotes
            /// </summary>
            public static GDStringNode MultilineString(string content) => String(content, GDStringBoundingChar.TripleDoubleQuotas);

            /// <summary>
            /// Creates a multiline string using triple single quotes
            /// </summary>
            public static GDStringNode MultilineStringSingleQuote(string content) => String(content, GDStringBoundingChar.TripleSingleQuotas);
        }
    }
}
