using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Incremental
{
    [TestClass]
    public class AstValidatorTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Basic Validation Tests

        [TestMethod]
        public void Validate_ValidTree_ReturnsValid()
        {
            var code = "class_name Player\n\nvar health: int = 100\n\nfunc attack():\n    pass\n";
            var tree = _reader.ParseFileContent(code);

            // Validate with normalized text (using tree's output)
            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
            result.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public void Validate_NullTree_ReturnsInvalid()
        {
            var result = GDAstValidator.Validate(null);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("null"));
        }

        [TestMethod]
        public void Validate_EmptyTree_ReturnsValid()
        {
            var tree = new GDClassDeclaration();

            var result = GDAstValidator.Validate(tree);

            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Text Equivalence Tests

        [TestMethod]
        public void Validate_TextMatches_ReturnsValid()
        {
            var code = "func test():\n    pass";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_TextDiffers_ReturnsInvalid()
        {
            var code = "func test():\n    pass";
            var tree = _reader.ParseFileContent(code);

            // Validate against different text
            var result = GDAstValidator.Validate(tree, "func different():\n    pass");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Text mismatch"));
        }

        [TestMethod]
        public void Validate_WithoutExpectedText_SkipsTextCheck()
        {
            var code = "func test():\n    pass";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree);

            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Parent-Child Relation Tests

        [TestMethod]
        public void Validate_ClonedTree_HasValidParentRelations()
        {
            var code = "var x = 1\nfunc test():\n    var y = 2\n";
            var tree = _reader.ParseFileContent(code);
            var cloned = (GDClassDeclaration)tree.Clone();

            var result = GDAstValidator.Validate(cloned, cloned.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        [TestMethod]
        public void Validate_DeeplyNested_HasValidParentRelations()
        {
            var code = "func test():\n    if true:\n        while x > 0:\n            for i in range(10):\n                if y:\n                    pass\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        #endregion

        #region Token Uniqueness Tests

        [TestMethod]
        public void Validate_NoDuplicateTokens_ReturnsValid()
        {
            var code = "var a = 1\nvar b = 2\nvar c = 3\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        #endregion

        #region Complex Code Tests

        [TestMethod]
        public void Validate_ComplexClass_ReturnsValid()
        {
            var code = "class_name ComplexClass\nextends Node2D\n\n@export var speed: float = 100.0\n\nenum State { IDLE, WALKING, RUNNING }\n\nvar _state: State = State.IDLE\n\nfunc _ready():\n    pass\n\nfunc move(delta: float):\n    match _state:\n        State.IDLE:\n            pass\n        State.WALKING:\n            position.x += speed * delta\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        [TestMethod]
        public void Validate_MultipleInnerClasses_ReturnsValid()
        {
            var code = "class_name Outer\n\nclass Inner1:\n    var x = 1\n    func foo():\n        pass\n\nclass Inner2:\n    var y = 2\n    func bar():\n        pass\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        #endregion

        #region Structure Comparison Tests

        [TestMethod]
        public void CompareStructure_IdenticalTrees_ReturnsNoDifferences()
        {
            var code = "func test():\n    pass\n";
            var tree1 = _reader.ParseFileContent(code);
            var tree2 = _reader.ParseFileContent(code);

            var differences = GDAstValidator.CompareStructure(tree1, tree2);

            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void CompareStructure_DifferentTrees_ReturnsDifferences()
        {
            var code1 = "func test():\n    pass";
            var code2 = "func other():\n    pass";

            var tree1 = _reader.ParseFileContent(code1);
            var tree2 = _reader.ParseFileContent(code2);

            var differences = GDAstValidator.CompareStructure(tree1, tree2);

            differences.Should().NotBeEmpty();
        }

        [TestMethod]
        public void CompareStructure_ClonedTree_ReturnsNoDifferences()
        {
            var code = "class_name Test\nvar x = 1\nfunc foo():\n    pass\n";
            var tree = _reader.ParseFileContent(code);
            var cloned = (GDClassDeclaration)tree.Clone();

            var differences = GDAstValidator.CompareStructure(tree, cloned);

            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void CompareStructure_NullTree1_ReturnsDifference()
        {
            var tree = _reader.ParseFileContent("func test():\n    pass");

            var differences = GDAstValidator.CompareStructure(null, tree);

            differences.Should().Contain(d => d.Contains("null"));
        }

        [TestMethod]
        public void CompareStructure_NullTree2_ReturnsDifference()
        {
            var tree = _reader.ParseFileContent("func test():\n    pass");

            var differences = GDAstValidator.CompareStructure(tree, null);

            differences.Should().Contain(d => d.Contains("null"));
        }

        [TestMethod]
        public void CompareStructure_BothNull_ReturnsNoDifferences()
        {
            var differences = GDAstValidator.CompareStructure(null, null);

            differences.Should().BeEmpty();
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Validate_EmptyMethod_ReturnsValid()
        {
            var code = "func empty():\n    pass";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_MethodWithComments_ReturnsValid()
        {
            var code = "# This is a comment\nfunc test():\n    # Another comment\n    pass  # Inline comment\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        [TestMethod]
        public void Validate_StringLiterals_ReturnsValid()
        {
            var code = "var s1 = \"hello\"\nvar s2 = 'world'\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        [TestMethod]
        public void Validate_LambdaExpressions_ReturnsValid()
        {
            var code = "var f = func(x): return x * 2\nvar g = func(a, b):\n    return a + b\n";
            var tree = _reader.ParseFileContent(code);

            var result = GDAstValidator.Validate(tree, tree.ToString());

            result.IsValid.Should().BeTrue(string.Join("\n", result.Errors));
        }

        #endregion
    }
}
