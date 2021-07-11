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

            public static GDSpace Space(int count = 1) => new GDSpace() { Sequence = new string(' ', count) };
            public static GDSpace Space(string whiteSpace) => new GDSpace() { Sequence = whiteSpace };
            public static GDNewLine NewLine() => new GDNewLine();
            public static GDIntendation Intendation(int count = 0) => new GDIntendation() { Sequence = new string('\t', count), LineIntendationThreshold = count };
            public static GDComment Comment(string comment) => new GDComment() { Sequence = comment };

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
