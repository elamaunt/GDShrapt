using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public static partial class GD
    {
        public static class Type
        {
            public static GDSingleTypeNode Single(string name) => new GDSingleTypeNode()
            {
                Type = new GDType() { Sequence = name }
            };

            public static GDSingleTypeNode Single(GDType type) => new GDSingleTypeNode()
            {
                Type = type
            };

            public static GDArrayTypeNode Array() => new GDArrayTypeNode();
            public static GDArrayTypeNode Array(Func<GDArrayTypeNode, GDArrayTypeNode> setup) => setup(new GDArrayTypeNode());
            public static GDArrayTypeNode Array(params GDSyntaxToken[] unsafeTokens) => new GDArrayTypeNode() { FormTokensSetter = unsafeTokens };

            public static GDArrayTypeNode Array(string elementTypeName) => new GDArrayTypeNode()
            {
                ArrayKeyword = new GDArrayKeyword(),
                SquareOpenBracket = new GDSquareOpenBracket(),
                InnerType = Single(elementTypeName),
                SquareCloseBracket = new GDSquareCloseBracket()
            };

            public static GDArrayTypeNode Array(GDTypeNode elementType) => new GDArrayTypeNode()
            {
                ArrayKeyword = new GDArrayKeyword(),
                SquareOpenBracket = new GDSquareOpenBracket(),
                InnerType = elementType,
                SquareCloseBracket = new GDSquareCloseBracket()
            };

            /// <summary>
            /// Creates an untyped Array type node (just "Array")
            /// </summary>
            public static GDArrayTypeNode UntypedArray() => new GDArrayTypeNode()
            {
                ArrayKeyword = new GDArrayKeyword()
            };

            public static GDDictionaryTypeNode Dictionary() => new GDDictionaryTypeNode();
            public static GDDictionaryTypeNode Dictionary(Func<GDDictionaryTypeNode, GDDictionaryTypeNode> setup) => setup(new GDDictionaryTypeNode());
            public static GDDictionaryTypeNode Dictionary(params GDSyntaxToken[] unsafeTokens) => new GDDictionaryTypeNode() { FormTokensSetter = unsafeTokens };

            public static GDDictionaryTypeNode Dictionary(string keyType, string valueType) => new GDDictionaryTypeNode()
            {
                DictionaryKeyword = new GDDictionaryKeyword(),
                SquareOpenBracket = new GDSquareOpenBracket(),
                KeyType = Single(keyType),
                Comma = new GDComma(),
                [4] = Syntax.Space(),
                ValueType = Single(valueType),
                SquareCloseBracket = new GDSquareCloseBracket()
            };

            public static GDDictionaryTypeNode Dictionary(GDTypeNode keyType, GDTypeNode valueType) => new GDDictionaryTypeNode()
            {
                DictionaryKeyword = new GDDictionaryKeyword(),
                SquareOpenBracket = new GDSquareOpenBracket(),
                KeyType = keyType,
                Comma = new GDComma(),
                [4] = Syntax.Space(),
                ValueType = valueType,
                SquareCloseBracket = new GDSquareCloseBracket()
            };

            /// <summary>
            /// Creates an untyped Dictionary type node (just "Dictionary")
            /// </summary>
            public static GDDictionaryTypeNode UntypedDictionary() => new GDDictionaryTypeNode()
            {
                DictionaryKeyword = new GDDictionaryKeyword()
            };
        }
    }
}
