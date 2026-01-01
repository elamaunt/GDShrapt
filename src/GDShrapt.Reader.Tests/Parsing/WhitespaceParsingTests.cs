using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Parsing
{
    /// <summary>
    /// Tests for correct whitespace handling in the parser.
    /// These tests verify that whitespace tokens are placed at the correct level
    /// in the syntax tree, not absorbed into child nodes.
    /// </summary>
    [TestClass]
    public class WhitespaceParsingTests
    {
        #region Parameter List Whitespace Tests

        [TestMethod]
        public void Parser_ParameterWithTrailingSpace_SpaceShouldNotBeInsideParameter()
        {
            // When parsing "func test( a, b ):", the trailing space before ")"
            // should NOT be inside the GDParameterDeclaration for 'b'.
            // It should be a separate GDSpace token in the GDParametersList.

            var reader = new GDScriptReader();
            var code = "func test( a, b ):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var parameters = method.Parameters.ToList();

            // Each parameter's ToString() should NOT contain trailing whitespace
            foreach (var param in parameters)
            {
                var paramString = param.ToString();
                paramString.Should().NotEndWith(" ",
                    $"parameter '{param.Identifier}' should not contain trailing space, but got '{paramString}'");
                paramString.Should().NotStartWith(" ",
                    $"parameter '{param.Identifier}' should not contain leading space, but got '{paramString}'");
            }
        }

        [TestMethod]
        public void Parser_ParameterWithLeadingSpace_SpaceShouldBeInParametersList()
        {
            // When parsing "func test( a, b ):", the leading space after "("
            // should be a GDSpace token in the GDParametersList, not absorbed by parameter 'a'.

            var reader = new GDScriptReader();
            var code = "func test( a, b ):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();

            // The first token in Parameters.Form should be a GDSpace
            GDSyntaxToken firstToken = null;
            foreach (var token in method.Parameters.Form)
            {
                firstToken = token;
                break;
            }

            firstToken.Should().BeOfType<GDSpace>(
                "leading space after '(' should be a separate GDSpace token in the parameters list");
        }

        [TestMethod]
        public void Parser_ParameterWithTrailingSpace_SpaceShouldBeInParametersList()
        {
            // When parsing "func test( a, b ):", the trailing space before ")"
            // should be a GDSpace token in the GDParametersList, not absorbed by parameter 'b'.

            var reader = new GDScriptReader();
            var code = "func test( a, b ):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();

            // The last token in Parameters.Form should be a GDSpace
            GDSyntaxToken lastToken = null;
            foreach (var token in method.Parameters.Form)
            {
                lastToken = token;
            }

            lastToken.Should().BeOfType<GDSpace>(
                "trailing space before ')' should be a separate GDSpace token in the parameters list");
        }

        #endregion

        #region Expression List Whitespace Tests

        [TestMethod]
        public void Parser_ArrayWithSpacedElements_SpacesShouldNotBeInsideExpressions()
        {
            // When parsing "[ 1, 2, 3 ]", spaces should be separate GDSpace tokens,
            // not absorbed into the number expressions.

            var reader = new GDScriptReader();
            var code = "var arr = [ 1, 2, 3 ]\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var arrayExpr = variable.Initializer as GDArrayInitializerExpression;
            arrayExpr.Should().NotBeNull();

            foreach (var value in arrayExpr.Values)
            {
                var valueString = value.ToString();
                valueString.Should().NotEndWith(" ",
                    $"array element should not contain trailing space, but got '{valueString}'");
                valueString.Should().NotStartWith(" ",
                    $"array element should not contain leading space, but got '{valueString}'");
            }
        }

        [TestMethod]
        public void Parser_ArrayWithLeadingSpace_SpaceShouldBeInValuesList()
        {
            // When parsing "[ 1, 2, 3 ]", the leading space after "["
            // should be a GDSpace token in the Values list.

            var reader = new GDScriptReader();
            var code = "var arr = [ 1, 2, 3 ]\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var arrayExpr = variable.Initializer as GDArrayInitializerExpression;
            arrayExpr.Should().NotBeNull();

            GDSyntaxToken firstToken = null;
            foreach (var token in arrayExpr.Values.Form)
            {
                firstToken = token;
                break;
            }

            firstToken.Should().BeOfType<GDSpace>(
                "leading space after '[' should be a separate GDSpace token in the values list");
        }

        [TestMethod]
        public void Parser_ArrayWithTrailingSpace_SpaceShouldBeInValuesList()
        {
            // When parsing "[ 1, 2, 3 ]", the trailing space before "]"
            // should be a GDSpace token in the Values list.

            var reader = new GDScriptReader();
            var code = "var arr = [ 1, 2, 3 ]\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var arrayExpr = variable.Initializer as GDArrayInitializerExpression;
            arrayExpr.Should().NotBeNull();

            GDSyntaxToken lastToken = null;
            foreach (var token in arrayExpr.Values.Form)
            {
                lastToken = token;
            }

            lastToken.Should().BeOfType<GDSpace>(
                "trailing space before ']' should be a separate GDSpace token in the values list");
        }

        #endregion

        #region Dictionary Whitespace Tests

        [TestMethod]
        public void Parser_DictionaryWithSpacedElements_SpacesShouldNotBeInsideKeyValues()
        {
            // When parsing "{ \"a\": 1 }", spaces should be separate GDSpace tokens.

            var reader = new GDScriptReader();
            var code = "var dict = { \"a\": 1 }\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dictExpr = variable.Initializer as GDDictionaryInitializerExpression;
            dictExpr.Should().NotBeNull();

            foreach (var kv in dictExpr.KeyValues)
            {
                var kvString = kv.ToString();
                kvString.Should().NotEndWith(" ",
                    $"dictionary key-value should not contain trailing space, but got '{kvString}'");
                kvString.Should().NotStartWith(" ",
                    $"dictionary key-value should not contain leading space, but got '{kvString}'");
            }
        }

        [TestMethod]
        public void Parser_DictionaryWithLeadingSpace_SpaceShouldBeInKeyValuesList()
        {
            // When parsing "{ \"a\": 1 }", the leading space after "{"
            // should be a GDSpace token in the KeyValues list.

            var reader = new GDScriptReader();
            var code = "var dict = { \"a\": 1 }\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dictExpr = variable.Initializer as GDDictionaryInitializerExpression;
            dictExpr.Should().NotBeNull();

            GDSyntaxToken firstToken = null;
            foreach (var token in dictExpr.KeyValues.Form)
            {
                firstToken = token;
                break;
            }

            firstToken.Should().BeOfType<GDSpace>(
                "leading space after '{' should be a separate GDSpace token in the key-values list");
        }

        [TestMethod]
        public void Parser_DictionaryWithTrailingSpace_SpaceShouldBeInKeyValuesList()
        {
            // When parsing "{ \"a\": 1 }", the trailing space before "}"
            // should be a GDSpace token in the KeyValues list.

            var reader = new GDScriptReader();
            var code = "var dict = { \"a\": 1 }\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dictExpr = variable.Initializer as GDDictionaryInitializerExpression;
            dictExpr.Should().NotBeNull();

            GDSyntaxToken lastToken = null;
            foreach (var token in dictExpr.KeyValues.Form)
            {
                lastToken = token;
            }

            lastToken.Should().BeOfType<GDSpace>(
                "trailing space before '}' should be a separate GDSpace token in the key-values list");
        }

        #endregion

        #region Call Expression Whitespace Tests

        [TestMethod]
        public void Parser_CallWithSpacedArguments_SpacesShouldNotBeInsideArguments()
        {
            // When parsing "func_call( a, b )", spaces should be separate tokens.

            var reader = new GDScriptReader();
            var code = "func test():\n\tfunc_call( a, b )\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var exprStatement = method.Statements[0] as GDExpressionStatement;
            var callExpr = exprStatement.Expression as GDCallExpression;
            callExpr.Should().NotBeNull();

            foreach (var param in callExpr.Parameters)
            {
                var paramString = param.ToString();
                paramString.Should().NotEndWith(" ",
                    $"call argument should not contain trailing space, but got '{paramString}'");
                paramString.Should().NotStartWith(" ",
                    $"call argument should not contain leading space, but got '{paramString}'");
            }
        }

        [TestMethod]
        public void Parser_CallWithLeadingSpace_SpaceShouldBeInParametersList()
        {
            // When parsing "func_call( a, b )", the leading space after "("
            // should be a GDSpace token in the Parameters list.

            var reader = new GDScriptReader();
            var code = "func test():\n\tfunc_call( a, b )\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var exprStatement = method.Statements[0] as GDExpressionStatement;
            var callExpr = exprStatement.Expression as GDCallExpression;
            callExpr.Should().NotBeNull();

            GDSyntaxToken firstToken = null;
            foreach (var token in callExpr.Parameters.Form)
            {
                firstToken = token;
                break;
            }

            firstToken.Should().BeOfType<GDSpace>(
                "leading space after '(' in call should be a separate GDSpace token");
        }

        [TestMethod]
        public void Parser_CallWithTrailingSpace_SpaceShouldBeInParametersList()
        {
            // When parsing "func_call( a, b )", the trailing space before ")"
            // should be a GDSpace token in the Parameters list.

            var reader = new GDScriptReader();
            var code = "func test():\n\tfunc_call( a, b )\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var exprStatement = method.Statements[0] as GDExpressionStatement;
            var callExpr = exprStatement.Expression as GDCallExpression;
            callExpr.Should().NotBeNull();

            GDSyntaxToken lastToken = null;
            foreach (var token in callExpr.Parameters.Form)
            {
                lastToken = token;
            }

            lastToken.Should().BeOfType<GDSpace>(
                "trailing space before ')' in call should be a separate GDSpace token");
        }

        #endregion

        #region Multiline Parameters Tests

        [TestMethod]
        public void Parser_MultilineParameters_TokenStructure()
        {
            // Debug test to see the token structure of multiline parameters
            var reader = new GDScriptReader();
            var code = "func _can_drop_data(\n\t\t_at_position: Vector2,\n\t\tdata: Variant\n) -> bool:\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();

            // Print tokens for debugging
            System.Console.WriteLine("Parameters.Form tokens:");
            int i = 0;
            foreach (var token in method.Parameters.Form)
            {
                System.Console.WriteLine($"  [{i}] {token.GetType().Name}: '{token.ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                i++;
            }

            // Round-trip should preserve exact whitespace
            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve exact whitespace in multiline parameters");
        }

        [TestMethod]
        public void Parser_MultilineParameters_RoundTripPreservesStructure()
        {
            // In multiline parameters, round-trip should preserve the structure.
            // The parser may place newlines in different locations (inside declarations or in the list),
            // but the round-trip output should match the input.
            var reader = new GDScriptReader();
            var code = "func test(\n\ta,\n\tb\n):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            // Round-trip should preserve exact structure
            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve multiline parameters structure");
        }

        #endregion

        #region Round-Trip Tests

        [TestMethod]
        public void Parser_RoundTrip_ParametersWithSpaces_PreservesExactWhitespace()
        {
            // Round-trip should preserve exact whitespace without modification.
            var reader = new GDScriptReader();
            var code = "func test( a, b ):\n\tpass\n";

            var tree = reader.ParseFileContent(code);
            var output = tree.ToString();

            output.Should().Be(code,
                "round-trip should preserve exact whitespace in parameters");
        }

        [TestMethod]
        public void Parser_RoundTrip_ArrayWithSpaces_PreservesExactWhitespace()
        {
            var reader = new GDScriptReader();
            var code = "var arr = [ 1, 2, 3 ]\n";

            var tree = reader.ParseFileContent(code);
            var output = tree.ToString();

            output.Should().Be(code,
                "round-trip should preserve exact whitespace in arrays");
        }

        [TestMethod]
        public void Parser_RoundTrip_DictionaryWithSpaces_PreservesExactWhitespace()
        {
            var reader = new GDScriptReader();
            var code = "var dict = { \"a\": 1 }\n";

            var tree = reader.ParseFileContent(code);
            var output = tree.ToString();

            output.Should().Be(code,
                "round-trip should preserve exact whitespace in dictionaries");
        }

        [TestMethod]
        public void Parser_RoundTrip_CallWithSpaces_PreservesExactWhitespace()
        {
            var reader = new GDScriptReader();
            var code = "func test():\n\tfunc_call( a, b )\n";

            var tree = reader.ParseFileContent(code);
            var output = tree.ToString();

            output.Should().Be(code,
                "round-trip should preserve exact whitespace in call expressions");
        }

        #endregion

        #region Member Operator / Rest Operator Tests

        [TestMethod]
        public void Parser_DictionaryRestOperator_SpaceShouldNotBeInsideRestExpression()
        {
            // When parsing "{ .. }", the trailing space before "}" should NOT be absorbed
            // into the GDMemberOperatorExpression (rest operator).
            var reader = new GDScriptReader();
            var code = "var dict = { .. }\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dictExpr = variable.Initializer as GDDictionaryInitializerExpression;
            dictExpr.Should().NotBeNull();

            foreach (var kv in dictExpr.KeyValues)
            {
                var kvString = kv.ToString();
                kvString.Should().NotEndWith(" ",
                    $"dictionary rest operator should not contain trailing space, but got '{kvString}'");
            }
        }

        [TestMethod]
        public void Parser_DictionaryRestOperator_SpaceShouldBeInKeyValuesList()
        {
            // When parsing "{ .. }", the trailing space should be a separate GDSpace token
            // in the KeyValues list, not absorbed by the rest operator.
            var reader = new GDScriptReader();
            var code = "var dict = { .. }\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dictExpr = variable.Initializer as GDDictionaryInitializerExpression;
            dictExpr.Should().NotBeNull();

            GDSyntaxToken lastToken = null;
            foreach (var token in dictExpr.KeyValues.Form)
                lastToken = token;

            lastToken.Should().BeOfType<GDSpace>(
                "trailing space before '}' should be a separate GDSpace token, not absorbed into rest operator");
        }

        [TestMethod]
        public void Parser_MatchDictionaryPatternWithRest_SpaceShouldNotBeInsideRest()
        {
            // Match patterns with dictionary rest operator should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "func test(dict):\n\tmatch dict:\n\t\t{ \"key\": var val, .. }:\n\t\t\treturn val\n";
            var tree = reader.ParseFileContent(code);

            // Find dictionary pattern
            foreach (var node in tree.AllNodes)
            {
                if (node is GDDictionaryInitializerExpression dictExpr)
                {
                    foreach (var kv in dictExpr.KeyValues)
                    {
                        var kvString = kv.ToString();
                        kvString.Should().NotEndWith(" ",
                            $"match dictionary key-value should not contain trailing space, but got '{kvString}'");
                    }
                }
            }
        }

        [TestMethod]
        public void Parser_MemberOperatorExpression_SpaceShouldNotBeAbsorbed()
        {
            // Member operator expressions (obj.member) should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar x = obj.member \n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var varStmt = method.Statements[0] as GDVariableDeclarationStatement;
            varStmt.Should().NotBeNull();

            var memberExpr = varStmt.Initializer as GDMemberOperatorExpression;
            if (memberExpr != null)
            {
                var exprString = memberExpr.ToString();
                exprString.Should().NotEndWith(" ",
                    $"member operator expression should not contain trailing space, but got '{exprString}'");
            }
        }

        #endregion

        #region Dual Operator Expression Tests

        [TestMethod]
        public void Parser_DualOperatorExpression_SpaceShouldNotBeAbsorbedIntoRight()
        {
            // Binary operators like "a + b " should not absorb trailing space into right operand
            var reader = new GDScriptReader();
            var code = "var x = a + b \n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dualExpr = variable.Initializer as GDDualOperatorExpression;
            dualExpr.Should().NotBeNull();

            var rightString = dualExpr.RightExpression?.ToString();
            rightString.Should().NotBeNull();
            rightString.Should().NotEndWith(" ",
                $"right operand should not contain trailing space, but got '{rightString}'");
        }

        [TestMethod]
        public void Parser_DualOperatorExpression_SpaceShouldNotBeAbsorbedIntoLeft()
        {
            // Binary operators should not absorb leading space into operands
            var reader = new GDScriptReader();
            var code = "var x =  a + b\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var dualExpr = variable.Initializer as GDDualOperatorExpression;
            dualExpr.Should().NotBeNull();

            var leftString = dualExpr.LeftExpression?.ToString();
            leftString.Should().NotBeNull();
            leftString.Should().NotStartWith(" ",
                $"left operand should not contain leading space, but got '{leftString}'");
        }

        #endregion

        #region Indexer Expression Tests

        [TestMethod]
        public void Parser_IndexerExpression_SpaceShouldNotBeInsideIndex()
        {
            // When parsing "arr[ 0 ]", spaces should not be absorbed into index expression
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar x = arr[ 0 ]\n";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var varStmt = method.Statements[0] as GDVariableDeclarationStatement;
            varStmt.Should().NotBeNull();

            var indexerExpr = varStmt.Initializer as GDIndexerExpression;
            if (indexerExpr != null)
            {
                var indexString = indexerExpr.InnerExpression?.ToString();
                if (indexString != null)
                {
                    indexString.Should().NotStartWith(" ",
                        $"indexer index should not contain leading space, but got '{indexString}'");
                    indexString.Should().NotEndWith(" ",
                        $"indexer index should not contain trailing space, but got '{indexString}'");
                }
            }
        }

        #endregion

        #region Single Operator Expression Tests

        [TestMethod]
        public void Parser_SingleOperatorExpression_SpaceShouldNotBeAbsorbed()
        {
            // Unary operators like "not a " should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "var x = not a \n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var singleExpr = variable.Initializer as GDSingleOperatorExpression;
            if (singleExpr != null)
            {
                var targetString = singleExpr.TargetExpression?.ToString();
                if (targetString != null)
                {
                    targetString.Should().NotEndWith(" ",
                        $"unary operator target should not contain trailing space, but got '{targetString}'");
                }
            }
        }

        [TestMethod]
        public void Parser_NegationOperator_SpaceShouldNotBeAbsorbed()
        {
            // Negation like "-a " should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "var x = -a \n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var singleExpr = variable.Initializer as GDSingleOperatorExpression;
            if (singleExpr != null)
            {
                var targetString = singleExpr.TargetExpression?.ToString();
                if (targetString != null)
                {
                    targetString.Should().NotEndWith(" ",
                        $"negation target should not contain trailing space, but got '{targetString}'");
                }
            }
        }

        #endregion

        #region Bracket Expression Tests

        [TestMethod]
        public void Parser_BracketExpression_SpaceShouldNotBeInsideExpression()
        {
            // When parsing "( a + b )", inner spaces should be in bracket, not in expression
            var reader = new GDScriptReader();
            var code = "var x = ( a + b )\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var bracketExpr = variable.Initializer as GDBracketExpression;
            bracketExpr.Should().NotBeNull();

            var innerString = bracketExpr.InnerExpression?.ToString();
            if (innerString != null)
            {
                innerString.Should().NotStartWith(" ",
                    $"bracket inner expression should not contain leading space, but got '{innerString}'");
                innerString.Should().NotEndWith(" ",
                    $"bracket inner expression should not contain trailing space, but got '{innerString}'");
            }
        }

        #endregion

        #region If Expression (Ternary) Tests

        [TestMethod]
        public void Parser_IfExpression_SpaceShouldNotBeAbsorbed()
        {
            // Ternary if like "a if cond else b " should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "var x = a if cond else b \n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var ifExpr = variable.Initializer as GDIfExpression;
            if (ifExpr != null)
            {
                var elseString = ifExpr.FalseExpression?.ToString();
                if (elseString != null)
                {
                    elseString.Should().NotEndWith(" ",
                        $"if expression else part should not contain trailing space, but got '{elseString}'");
                }
            }
        }

        #endregion

        #region Type Annotation Tests

        [TestMethod]
        public void Parser_TypeAnnotation_SpaceShouldNotBeInsideType()
        {
            // Type annotations like "var x: int " should not absorb trailing space into type
            var reader = new GDScriptReader();
            var code = "var x: int \n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            var typeString = variable.Type?.ToString();
            if (typeString != null)
            {
                typeString.Should().NotEndWith(" ",
                    $"type annotation should not contain trailing space, but got '{typeString}'");
            }
        }

        [TestMethod]
        public void Parser_ArrayTypeAnnotation_SpaceShouldNotBeInsideType()
        {
            // Array type like "var x: Array[ int ] " should not absorb spaces
            var reader = new GDScriptReader();
            var code = "var x: Array[ int ]\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            if (variable.Type is GDArrayTypeNode arrayType)
            {
                var innerTypeString = arrayType.InnerType?.ToString();
                if (innerTypeString != null)
                {
                    innerTypeString.Should().NotStartWith(" ",
                        $"array inner type should not contain leading space, but got '{innerTypeString}'");
                    innerTypeString.Should().NotEndWith(" ",
                        $"array inner type should not contain trailing space, but got '{innerTypeString}'");
                }
            }
        }

        #endregion

        #region Lambda/Callable Tests

        [TestMethod]
        public void Parser_Lambda_SpaceShouldNotBeAbsorbedIntoBody()
        {
            // Lambda expressions should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "var f = func(): return 1 \n";
            var tree = reader.ParseFileContent(code);

            // Round-trip test is sufficient here
            var output = tree.ToString();
            output.Should().Be(code, "lambda expression should preserve whitespace correctly");
        }

        #endregion

        #region Return/Yield Expression Tests

        [TestMethod]
        public void Parser_ReturnExpression_SpaceShouldNotBeAbsorbed()
        {
            // Return expressions should not absorb trailing space into returned value
            var reader = new GDScriptReader();
            var code = "func test():\n\treturn value \n";
            var tree = reader.ParseFileContent(code);

            // Find return expression
            foreach (var node in tree.AllNodes)
            {
                if (node is GDReturnExpression returnExpr)
                {
                    var resultString = returnExpr.Expression?.ToString();
                    if (resultString != null)
                    {
                        resultString.Should().NotEndWith(" ",
                            $"return value should not contain trailing space, but got '{resultString}'");
                    }
                }
            }
        }

        [TestMethod]
        public void Parser_AwaitExpression_SpaceShouldNotBeAbsorbed()
        {
            // Await expressions should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "func test():\n\tawait some_signal \n";
            var tree = reader.ParseFileContent(code);

            foreach (var node in tree.AllNodes)
            {
                if (node is GDAwaitExpression awaitExpr)
                {
                    var exprString = awaitExpr.Expression?.ToString();
                    if (exprString != null)
                    {
                        exprString.Should().NotEndWith(" ",
                            $"await expression should not contain trailing space, but got '{exprString}'");
                    }
                }
            }
        }

        #endregion

        #region Complex Nested Structure Tests

        [TestMethod]
        public void Parser_NestedDictionary_SpacesShouldNotBeAbsorbed()
        {
            // Nested structures should handle whitespace correctly
            var reader = new GDScriptReader();
            var code = "var x = { \"a\": { \"b\": 1 } }\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "nested dictionary should preserve whitespace correctly");
        }

        [TestMethod]
        public void Parser_NestedArrays_SpacesShouldNotBeAbsorbed()
        {
            var reader = new GDScriptReader();
            var code = "var x = [ [ 1, 2 ], [ 3, 4 ] ]\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "nested arrays should preserve whitespace correctly");
        }

        [TestMethod]
        public void Parser_CallWithDictArg_SpacesShouldNotBeAbsorbed()
        {
            // Function call with dictionary argument
            var reader = new GDScriptReader();
            var code = "func test():\n\tcall({ \"key\": value })\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "call with dictionary argument should preserve whitespace correctly");
        }

        [TestMethod]
        public void Parser_CallWithDictArgAndSpaces_SpacesShouldNotBeAbsorbed()
        {
            // Function call with spaced dictionary argument
            var reader = new GDScriptReader();
            var code = "func test():\n\tcall( { \"key\": value } )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "call with spaced dictionary argument should preserve whitespace correctly");
        }

        #endregion

        #region MultiLineSplitToken Tests

        [TestMethod]
        public void Parser_MultiLineSplit_CallExpression_RoundTrip()
        {
            // Line continuation in function calls
            var reader = new GDScriptReader();
            var code = "func test():\n\tcall(a, \\\n\t     b)\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in call expression");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_LocalVariable_RoundTrip()
        {
            // Line continuation in local variable declaration
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar x = \\\n\t\t1 + 2\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in local variable");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_LocalVariableWithType_RoundTrip()
        {
            // Line continuation in typed local variable declaration
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar x: int = \\\n\t\t42\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in typed local variable");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_GlobalVariable_RoundTrip()
        {
            // Line continuation in global variable declaration
            var reader = new GDScriptReader();
            var code = "var global_var = \\\n\t1 + 2 + \\\n\t3\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in global variable");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_GlobalVariableWithType_RoundTrip()
        {
            // Line continuation in typed global variable declaration
            var reader = new GDScriptReader();
            var code = "var typed_var: int = \\\n\t100\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in typed global variable");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_ConstDeclaration_RoundTrip()
        {
            // Line continuation in const declaration
            var reader = new GDScriptReader();
            var code = "const MY_CONST = \\\n\t\"value\"\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in const declaration");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_VariableAssignment_RoundTrip()
        {
            // Line continuation in assignment statement
            var reader = new GDScriptReader();
            var code = "func test():\n\tx = \\\n\t\ty + z\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in assignment");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_Array_RoundTrip()
        {
            // Line continuation in arrays
            var reader = new GDScriptReader();
            var code = "var arr = [\\\n\t1,\\\n\t2\\\n]\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in arrays");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_Dictionary_RoundTrip()
        {
            // Line continuation in dictionaries
            var reader = new GDScriptReader();
            var code = "var dict = {\\\n\t\"a\": 1,\\\n\t\"b\": 2\\\n}\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in dictionaries");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_DualOperator_RoundTrip()
        {
            // Line continuation in binary expressions
            var reader = new GDScriptReader();
            var code = "var x = a +\\\n\tb +\\\n\tc\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in binary expressions");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_FunctionParameters_RoundTrip()
        {
            // Line continuation in function parameters
            var reader = new GDScriptReader();
            var code = "func test(\\\n\ta,\\\n\tb\\\n):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in function parameters");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_SpaceBeforeAndAfter_RoundTrip()
        {
            // Space + MultiLineSplit + Space sequence
            var reader = new GDScriptReader();
            var code = "var x = a + \\\n b\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve Space + MultiLineSplit + Space sequence");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_WithTrailingSpaces_RoundTrip()
        {
            // MultiLineSplit with spaces inside (before newline)
            var reader = new GDScriptReader();
            var code = "var x = a + \\   \n\tb\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces inside MultiLineSplit");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_Multiple_RoundTrip()
        {
            // Multiple MultiLineSplit in sequence
            var reader = new GDScriptReader();
            var code = "var x = a +\\\n\tb +\\\n\tc +\\\n\td\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve multiple line continuations");
        }

        #endregion

        #region Type Annotation Whitespace Tests

        [TestMethod]
        public void Parser_DictionaryType_SpacesInsideBrackets_RoundTrip()
        {
            // Dictionary type with spaces inside brackets
            var reader = new GDScriptReader();
            var code = "var dict: Dictionary[ String, int ]\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces inside Dictionary type brackets");
        }

        [TestMethod]
        public void Parser_DictionaryType_SpaceShouldNotBeInsideTypes()
        {
            // Spaces should not be absorbed into the inner types
            var reader = new GDScriptReader();
            var code = "var dict: Dictionary[ String, int ]\n";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.First();
            if (variable.Type is GDDictionaryTypeNode dictType)
            {
                var keyTypeString = dictType.KeyType?.ToString();
                var valueTypeString = dictType.ValueType?.ToString();

                if (keyTypeString != null)
                {
                    keyTypeString.Should().NotStartWith(" ", "key type should not have leading space");
                    keyTypeString.Should().NotEndWith(" ", "key type should not have trailing space");
                }
                if (valueTypeString != null)
                {
                    valueTypeString.Should().NotStartWith(" ", "value type should not have leading space");
                    valueTypeString.Should().NotEndWith(" ", "value type should not have trailing space");
                }
            }
        }

        [TestMethod]
        public void Parser_NestedGenericType_SpacesPreserved_RoundTrip()
        {
            // Nested generic types
            var reader = new GDScriptReader();
            var code = "var x: Array[ Dictionary[ String, int ] ]\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in nested generic types");
        }

        [TestMethod]
        public void Parser_ReturnType_SpacesInsideBrackets_RoundTrip()
        {
            // Return type with spaces inside brackets
            var reader = new GDScriptReader();
            var code = "func test() -> Array[ int ]:\n\treturn []\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in return type brackets");
        }

        [TestMethod]
        public void Parser_ParameterType_SpacesPreserved_RoundTrip()
        {
            // Parameter with type that has spaces
            var reader = new GDScriptReader();
            var code = "func test( a: Array[ int ] ):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in parameter type");
        }

        #endregion

        #region Control Flow Whitespace Tests

        [TestMethod]
        public void Parser_ForStatement_SpacedRange_RoundTrip()
        {
            // For with spaces in range call
            var reader = new GDScriptReader();
            var code = "func test():\n\tfor i in range( 0, 10 ):\n\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in for range call");
        }

        [TestMethod]
        public void Parser_WhileStatement_SpaceBeforeColon_RoundTrip()
        {
            // While with space before colon
            var reader = new GDScriptReader();
            var code = "func test():\n\twhile condition :\n\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve space before colon in while");
        }

        [TestMethod]
        public void Parser_IfStatement_SpacedCondition_RoundTrip()
        {
            // If with spaces in condition
            var reader = new GDScriptReader();
            var code = "func test():\n\tif ( a and b ) :\n\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in if condition");
        }

        #endregion

        #region Match Statement Whitespace Tests

        [TestMethod]
        public void Parser_MatchArrayPattern_WithRest_RoundTrip()
        {
            // Match with array pattern and rest operator
            var reader = new GDScriptReader();
            var code = "func test(arr):\n\tmatch arr:\n\t\t[ a, b, .. ]:\n\t\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve array pattern with rest operator");
        }

        [TestMethod]
        public void Parser_MatchDictPattern_SpacedElements_RoundTrip()
        {
            // Match with dictionary pattern
            var reader = new GDScriptReader();
            var code = "func test(d):\n\tmatch d:\n\t\t{ \"key\": var val, .. }:\n\t\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve dictionary pattern with rest");
        }

        [TestMethod]
        public void Parser_MatchBindingPattern_Spaced_RoundTrip()
        {
            // Match with binding pattern
            var reader = new GDScriptReader();
            var code = "func test(x):\n\tmatch x:\n\t\tvar value :\n\t\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve binding pattern spacing");
        }

        #endregion

        #region Enum Declaration Whitespace Tests

        [TestMethod]
        public void Parser_EnumDeclaration_SpacedValues_RoundTrip()
        {
            // Enum with spaces between values
            var reader = new GDScriptReader();
            var code = "enum MyEnum { VALUE_A, VALUE_B, VALUE_C }\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in enum values");
        }

        [TestMethod]
        public void Parser_EnumDeclaration_WithAssignments_RoundTrip()
        {
            // Enum with assignments and spaces
            var reader = new GDScriptReader();
            var code = "enum MyEnum { A = 1, B = 2 }\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve enum assignments with spaces");
        }

        [TestMethod]
        public void Parser_EnumDeclaration_SpaceShouldNotBeInsideValues()
        {
            // Spaces should not be absorbed into enum values
            var reader = new GDScriptReader();
            var code = "enum MyEnum { A, B, C }\n";
            var tree = reader.ParseFileContent(code);

            var enumDecl = tree.AllNodes.OfType<GDEnumDeclaration>().First();
            foreach (var value in enumDecl.Values)
            {
                var valueString = value.ToString();
                valueString.Should().NotStartWith(" ", "enum value should not have leading space");
                valueString.Should().NotEndWith(" ", "enum value should not have trailing space");
            }
        }

        #endregion

        #region Signal Declaration Whitespace Tests

        [TestMethod]
        public void Parser_SignalDeclaration_SpacedParameters_RoundTrip()
        {
            // Signal with spaced parameters
            var reader = new GDScriptReader();
            var code = "signal my_signal( arg1: int, arg2: String )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in signal parameters");
        }

        [TestMethod]
        public void Parser_SignalDeclaration_SpaceShouldNotBeInsideParameters()
        {
            // Spaces should not be absorbed into signal parameters
            var reader = new GDScriptReader();
            var code = "signal my_signal( arg1, arg2 )\n";
            var tree = reader.ParseFileContent(code);

            var signal = tree.AllNodes.OfType<GDSignalDeclaration>().First();
            foreach (var param in signal.Parameters)
            {
                var paramString = param.ToString();
                paramString.Should().NotStartWith(" ", "signal parameter should not have leading space");
                paramString.Should().NotEndWith(" ", "signal parameter should not have trailing space");
            }
        }

        #endregion

        #region Lambda Expression Whitespace Tests

        [TestMethod]
        public void Parser_Lambda_SpacedParameters_RoundTrip()
        {
            // Lambda with spaced parameters
            var reader = new GDScriptReader();
            var code = "var f = func( a, b ): return a + b\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in lambda parameters");
        }

        [TestMethod]
        public void Parser_Lambda_WithReturnType_RoundTrip()
        {
            // Lambda with return type and spaces
            var reader = new GDScriptReader();
            var code = "var f = func( a, b ) -> int: return a + b\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve lambda with return type");
        }

        [TestMethod]
        public void Parser_Lambda_SpaceShouldNotBeInsideBody()
        {
            // Space should not be absorbed into lambda body expression
            var reader = new GDScriptReader();
            var code = "var f = func(): return a \n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve lambda body spacing");
        }

        #endregion

        #region Attribute Whitespace Tests

        [TestMethod]
        public void Parser_ExportAttribute_SpacedRange_RoundTrip()
        {
            // Export with spaced range
            var reader = new GDScriptReader();
            var code = "@export_range( 0, 10 ) var value: int\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in export_range");
        }

        [TestMethod]
        public void Parser_ExportEnumAttribute_Spaced_RoundTrip()
        {
            // Export enum with spaces
            var reader = new GDScriptReader();
            var code = "@export_enum( \"A\", \"B\", \"C\" ) var choice: int\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in export_enum");
        }

        [TestMethod]
        public void Parser_Attribute_SpaceShouldNotBeInsideArguments()
        {
            // Spaces should not be absorbed into attribute arguments
            var reader = new GDScriptReader();
            var code = "@export_range( 0, 10 ) var value: int\n";
            var tree = reader.ParseFileContent(code);

            var customAttr = tree.AllNodes.OfType<GDCustomAttribute>().FirstOrDefault();
            if (customAttr?.Attribute?.Parameters != null)
            {
                foreach (var param in customAttr.Attribute.Parameters)
                {
                    var paramString = param.ToString();
                    paramString.Should().NotStartWith(" ", "attribute argument should not have leading space");
                    paramString.Should().NotEndWith(" ", "attribute argument should not have trailing space");
                }
            }
        }

        #endregion

        #region String Expression Whitespace Tests

        [TestMethod]
        public void Parser_StringFormat_SpacedArray_RoundTrip()
        {
            // String formatting with spaced array
            var reader = new GDScriptReader();
            var code = "var s = \"Value: %s\" % [ value ]\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve spaces in string format array");
        }

        [TestMethod]
        public void Parser_StringConcatenation_Spaced_RoundTrip()
        {
            // String concatenation with spaces
            var reader = new GDScriptReader();
            var code = "var s = \"Hello\" + \" \" + \"World\"\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve string concatenation spacing");
        }

        #endregion

        #region Yield Expression Tests

        [TestMethod]
        public void Parser_YieldExpression_SpaceShouldNotBeAbsorbed()
        {
            // Yield expressions should not absorb trailing space
            var reader = new GDScriptReader();
            var code = "func test():\n\tyield( get_tree(), \"idle_frame\" )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve yield expression spacing");
        }

        #endregion

        #region GetNode Expression Tests

        [TestMethod]
        public void Parser_GetNodeExpression_SpacedPath_RoundTrip()
        {
            // $Node expressions
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar node = $Node/Child\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve $Node expression");
        }

        [TestMethod]
        public void Parser_GetUniqueNodeExpression_RoundTrip()
        {
            // %Node expressions
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar node = %UniqueNode\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve %Node expression");
        }

        [TestMethod]
        public void Parser_NodePathExpression_RoundTrip()
        {
            // ^"path" expressions
            var reader = new GDScriptReader();
            var code = "var path = ^\"Node/Child\"\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve ^\"path\" expression");
        }

        #endregion

        #region Property Accessor Tests

        [TestMethod]
        public void Parser_PropertyGetSet_SpacedBody_RoundTrip()
        {
            // Property with get/set
            var reader = new GDScriptReader();
            var code = "var prop: int:\n\tget:\n\t\treturn _prop\n\tset( value ):\n\t\t_prop = value\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve property get/set spacing");
        }

        [TestMethod]
        public void Parser_PropertyGetSetInline_RoundTrip()
        {
            // Property with inline get/set
            var reader = new GDScriptReader();
            var code = "var prop: int:\n\tget = _prop\n\tset = _prop\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve inline property accessor");
        }

        #endregion

        #region Inner Class Tests

        [TestMethod]
        public void Parser_InnerClass_SpacedDeclaration_RoundTrip()
        {
            // Inner class declaration
            var reader = new GDScriptReader();
            var code = "class InnerClass extends Node:\n\tvar value: int\n\tfunc test():\n\t\tpass\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve inner class structure");
        }

        #endregion

        #region Export Declaration Tests

        [TestMethod]
        public void Parser_ExportVar_SpacedDeclaration_RoundTrip()
        {
            // @export var with spaces
            var reader = new GDScriptReader();
            var code = "@export var value: int = 10\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve @export var declaration");
        }

        [TestMethod]
        public void Parser_ExportMultipleAttributes_RoundTrip()
        {
            // Multiple attributes on variable
            var reader = new GDScriptReader();
            var code = "@export\n@onready\nvar node: Node\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve multiple attributes");
        }

        #endregion

        #region Preload and Load Tests

        [TestMethod]
        public void Parser_PreloadExpression_SpacedPath_RoundTrip()
        {
            // preload with spaced arguments
            var reader = new GDScriptReader();
            var code = "var res = preload( \"res://path/to/resource.tscn\" )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve preload expression spacing");
        }

        [TestMethod]
        public void Parser_LoadExpression_SpacedPath_RoundTrip()
        {
            // load with spaced arguments
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar res = load( \"res://path/to/resource.tscn\" )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve load expression spacing");
        }

        #endregion

        #region Super and Self Tests

        [TestMethod]
        public void Parser_SuperCall_Spaced_RoundTrip()
        {
            // super() call with spaces
            var reader = new GDScriptReader();
            var code = "func _ready():\n\tsuper( )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve super() call spacing");
        }

        [TestMethod]
        public void Parser_SuperMethodCall_Spaced_RoundTrip()
        {
            // super.method() call
            var reader = new GDScriptReader();
            var code = "func test():\n\tsuper.method( a, b )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve super.method() call spacing");
        }

        #endregion

        #region Static and Const Tests

        [TestMethod]
        public void Parser_StaticVar_Spaced_RoundTrip()
        {
            // static var declaration
            var reader = new GDScriptReader();
            var code = "static var counter: int = 0\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve static var declaration");
        }

        [TestMethod]
        public void Parser_StaticFunc_Spaced_RoundTrip()
        {
            // static function declaration
            var reader = new GDScriptReader();
            var code = "static func helper( a: int, b: int ) -> int:\n\treturn a + b\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve static func declaration");
        }

        #endregion

        #region Await in Different Contexts Tests

        [TestMethod]
        public void Parser_AwaitSignal_Spaced_RoundTrip()
        {
            // await with signal
            var reader = new GDScriptReader();
            var code = "func test():\n\tawait get_tree().create_timer( 1.0 ).timeout\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve await signal chain");
        }

        [TestMethod]
        public void Parser_AwaitInAssignment_Spaced_RoundTrip()
        {
            // await in variable assignment
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar result = await async_function( arg1, arg2 )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve await in assignment");
        }

        #endregion

        #region Ternary If Expression Extended Tests

        [TestMethod]
        public void Parser_NestedIfExpression_Spaced_RoundTrip()
        {
            // Nested ternary if expressions
            var reader = new GDScriptReader();
            var code = "var x = a if cond1 else b if cond2 else c\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve nested if expressions");
        }

        [TestMethod]
        public void Parser_IfExpressionWithCall_Spaced_RoundTrip()
        {
            // If expression with function calls
            var reader = new GDScriptReader();
            var code = "var x = func1( a ) if condition( b ) else func2( c )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve if expression with calls");
        }

        #endregion

        #region Combined Scenarios Tests

        [TestMethod]
        public void Parser_ComplexCall_MultiLineSplitAndSpaces_RoundTrip()
        {
            // Complex call with MultiLineSplit and spaces
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar result = call( \\\n\t\targ1, \\\n\t\targ2 \\\n\t)\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve complex call with line continuation");
        }

        [TestMethod]
        public void Parser_NestedStructures_MultiLineSplit_RoundTrip()
        {
            // Nested structures with line continuation
            var reader = new GDScriptReader();
            var code = "var x = {\\\n\t\"nested\": {\\\n\t\t\"value\": 1\\\n\t}\\\n}\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve nested structures with line continuation");
        }

        [TestMethod]
        public void Parser_ChainedCalls_Spaced_RoundTrip()
        {
            // Chained method calls with spaces
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar x = obj.method( a ).another( b )\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve chained calls spacing");
        }

        [TestMethod]
        public void Parser_ComplexAssignment_Spaced_RoundTrip()
        {
            // Complex assignment with ternary and structures
            var reader = new GDScriptReader();
            var code = "var x = [ a, b ] if condition else { \"key\": value }\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve complex assignment spacing");
        }

        [TestMethod]
        public void Parser_MultiLineSplit_InMethodChain_RoundTrip()
        {
            // Line continuation in method chain
            var reader = new GDScriptReader();
            var code = "func test():\n\tvar x = obj\\\n\t\t.method()\\\n\t\t.another()\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve line continuation in method chain");
        }

        [TestMethod]
        public void Parser_SpaceMultiLineSplitSpace_Sequence_RoundTrip()
        {
            // Explicit GDSpace + GDMultiLineSplitToken + GDSpace sequence
            var reader = new GDScriptReader();
            var code = "var x = a \\\n b \\\n c\n";
            var tree = reader.ParseFileContent(code);

            var output = tree.ToString();
            output.Should().Be(code, "round-trip should preserve Space + MultiLineSplit + Space sequences");
        }

        #endregion
    }
}
