namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Syntax
        {
            public static GDIdentifier Identifier(string name) => new GDIdentifier() { Sequence = name };
            public static GDType Type(string name) => new GDType() { Sequence = name };
            public static GDString String(string value, bool multiline = false, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas) => new GDString()
            {
                Value = value,
                Multiline = multiline,
                BoundingChar = boundingChar
            };

            public static GDNumber Number(string stringValue) => new GDNumber() { ValueAsString = stringValue };
            public static GDNumber Number(int value) => new GDNumber() { ValueInt64 = value };
            public static GDNumber Number(long value) => new GDNumber() { ValueInt64 = value };
            public static GDNumber Number(double value) => new GDNumber() { ValueDouble = value };

            public static GDSpace OneSpace => Space();
            public static GDSpace Space(int count = 1) => new GDSpace() { Sequence = new string(' ', count) };
            public static GDSpace Space(string whiteSpace) => new GDSpace() { Sequence = whiteSpace };
            public static GDNewLine NewLine => new GDNewLine();
            public static GDIntendation Intendation(int count = 0) => new GDIntendation()
            { 
                Sequence = new string('\t', count),
                LineIntendationThreshold = count 
            };
            public static GDComment Comment(string comment) => new GDComment() { Sequence = comment };

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
            public static GDPathSpecifier PathSpecifier(string identifier) => new GDPathSpecifier() { Type = GDPathSpecifierType.Identifier, IdentifierValue = identifier };

            public static GDDualOperator DualOperator(GDDualOperatorType type) => new GDDualOperator()
            {
                OperatorType = type
            };

            public static GDSingleOperator SingleOperator(GDSingleOperatorType type) => new GDSingleOperator()
            {
                OperatorType = type
            };
        }
    }
}
