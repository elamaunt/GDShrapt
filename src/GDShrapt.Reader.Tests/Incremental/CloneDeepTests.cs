using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Incremental
{
    [TestClass]
    public class CloneDeepTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Basic Clone Tests

        [TestMethod]
        public void Clone_SimpleCode_PreservesText()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(code);
        }

        [TestMethod]
        public void Clone_CreatesNewInstance()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.Should().NotBeSameAs(tree);
        }

        [TestMethod]
        public void Clone_AllNodesAreDifferentInstances()
        {
            var code = @"
func test():
    var x = 1
    return x
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Collect all nodes from both trees
            var originalNodes = tree.AllNodes.ToHashSet();
            var clonedNodes = cloned.AllNodes.ToList();

            // No cloned node should be the same instance as original
            foreach (var node in clonedNodes)
            {
                originalNodes.Should().NotContain(node,
                    $"Cloned node {node.TypeName} should be a different instance");
            }
        }

        #endregion

        #region Parent Reference Tests

        [TestMethod]
        public void Clone_AllNodesHaveProperParentReferences()
        {
            var code = "class_name Test\nvar x = 1\nfunc foo():\n    pass\n";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Verify using validator (use tree.ToString() for text comparison)
            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_RootHasNoParent()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.Parent.Should().BeNull();
        }

        [TestMethod]
        public void Clone_ChildrenPointToClonedParent()
        {
            var code = @"
func test():
    var x = 1
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Get a method from cloned tree
            var method = cloned.Methods.First();

            // Its parent should be the cloned tree, not the original
            method.Parent.Should().NotBeSameAs(tree);
        }

        #endregion

        #region Modification Independence Tests

        [TestMethod]
        public void Clone_ModifyingCloneDoesNotAffectOriginal()
        {
            var code = @"
var x = 1
var y = 2
";
            var tree = _reader.ParseFileContent(code);
            var originalText = tree.ToString();

            var cloned = (GDClassDeclaration)tree.Clone();

            // Modify the cloned tree by removing a member
            var firstMember = cloned.Members.First();
            firstMember.RemoveFromParent();

            // Original should be unchanged
            tree.ToString().Should().Be(originalText);
            tree.Members.Count().Should().Be(2);
        }

        [TestMethod]
        public void Clone_ModifyingOriginalDoesNotAffectClone()
        {
            var code = @"
var x = 1
var y = 2
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();
            var clonedText = cloned.ToString();

            // Modify the original tree
            var firstMember = tree.Members.First();
            firstMember.RemoveFromParent();

            // Clone should be unchanged
            cloned.ToString().Should().Be(clonedText);
            cloned.Members.Count().Should().Be(2);
        }

        #endregion

        #region Complex Structure Tests

        [TestMethod]
        public void Clone_DeepNesting_WorksCorrectly()
        {
            var code = @"
func outer():
    if true:
        for i in range(10):
            while x:
                match y:
                    1:
                        pass
                    2:
                        pass
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Use tree.ToString() for comparison (parser normalizes line endings)
            cloned.ToString().Should().Be(tree.ToString());

            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_InnerClasses_WorksCorrectly()
        {
            var code = @"
class_name Outer

class Inner:
    var x = 1

    class DeepInner:
        var y = 2
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Use tree.ToString() for comparison (parser normalizes line endings)
            cloned.ToString().Should().Be(tree.ToString());

            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_PreservesAllTokenTypes()
        {
            var code = @"
# Comment
@export var health: int = 100
signal died
enum State { IDLE, WALK }

func test(x: int, y: float = 1.0) -> bool:
    var arr = [1, 2, 3]
    var dict = {""a"": 1}
    return true
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Use tree.ToString() for comparison (parser normalizes line endings)
            cloned.ToString().Should().Be(tree.ToString());

            // Count token types in both
            var originalTokenTypes = tree.AllTokens.Select(t => t.GetType().Name).ToList();
            var clonedTokenTypes = cloned.AllTokens.Select(t => t.GetType().Name).ToList();

            clonedTokenTypes.Should().BeEquivalentTo(originalTokenTypes);
        }

        #endregion

        #region Special Cases Tests

        [TestMethod]
        public void Clone_EmptyClass_WorksCorrectly()
        {
            var code = "";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(code);
        }

        [TestMethod]
        public void Clone_OnlyWhitespace_WorksCorrectly()
        {
            var code = "\n\n\n";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(code);
        }

        [TestMethod]
        public void Clone_ComplexExpressions_WorksCorrectly()
        {
            var code = @"
var result = (1 + 2) * 3 / 4 - 5 % 6 ** 7
var arr = [x for x in range(10) if x > 5]
var ternary = a if b else c
var call = obj.method(1, 2, named=3)
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Use tree.ToString() for comparison (parser normalizes line endings)
            cloned.ToString().Should().Be(tree.ToString());

            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_LambdasAndClosures_WorksCorrectly()
        {
            var code = @"
var f1 = func(): return 1
var f2 = func(x, y): return x + y
var f3 = func(arr):
    var sum = 0
    for x in arr:
        sum += x
    return sum
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            // Use tree.ToString() for comparison (parser normalizes line endings)
            cloned.ToString().Should().Be(tree.ToString());
        }

        #endregion

        #region Multiple Clone Tests

        [TestMethod]
        public void Clone_MultipleClones_AreIndependent()
        {
            var code = @"
var x = 1
func test():
    pass
";
            var tree = _reader.ParseFileContent(code);

            var clone1 = (GDClassDeclaration)tree.Clone();
            var clone2 = (GDClassDeclaration)tree.Clone();

            // All should be different instances
            clone1.Should().NotBeSameAs(tree);
            clone2.Should().NotBeSameAs(tree);
            clone1.Should().NotBeSameAs(clone2);

            // But have same content (use tree.ToString() due to line ending normalization)
            clone1.ToString().Should().Be(tree.ToString());
            clone2.ToString().Should().Be(tree.ToString());

            // Modify clone1, clone2 should be unaffected
            clone1.Members.First().RemoveFromParent();
            clone2.Members.Count().Should().Be(2);
        }

        [TestMethod]
        public void Clone_ChainedClones_WorkCorrectly()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            var clone1 = (GDClassDeclaration)tree.Clone();
            var clone2 = (GDClassDeclaration)clone1.Clone();
            var clone3 = (GDClassDeclaration)clone2.Clone();

            clone3.ToString().Should().Be(code);

            // All should be independent
            clone1.Should().NotBeSameAs(clone2);
            clone2.Should().NotBeSameAs(clone3);
        }

        #endregion

        #region Structural Comparison Tests

        [TestMethod]
        public void Clone_StructurallyEquivalentToOriginal()
        {
            var code = @"
class_name Test
var x = 1
func foo(a, b):
    return a + b
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            var differences = GDAstValidator.CompareStructure(tree, cloned);
            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void Clone_TokenCountMatchesOriginal()
        {
            var code = @"
func test():
    var x = 1
    var y = 2
    return x + y
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            var originalTokenCount = tree.AllTokens.Count();
            var clonedTokenCount = cloned.AllTokens.Count();

            clonedTokenCount.Should().Be(originalTokenCount);
        }

        [TestMethod]
        public void Clone_NodeCountMatchesOriginal()
        {
            // Now tests with elif/else - the cloning bug is fixed
            var code = "func test():\n    if a:\n        pass\n    elif b:\n        pass\n    else:\n        pass\n";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            var originalNodeCount = tree.AllNodes.Count();
            var clonedNodeCount = cloned.AllNodes.Count();

            clonedNodeCount.Should().Be(originalNodeCount);
        }

        #endregion

        #region Elif/Else Cloning Tests

        [TestMethod]
        public void Clone_IfElifElse_WorksCorrectly()
        {
            var code = @"
func test():
    if a:
        pass
    elif b:
        pass
    elif c:
        pass
    else:
        pass
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(tree.ToString());

            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_NestedIfElifElse_WorksCorrectly()
        {
            var code = @"
func test():
    if a:
        if x:
            pass
        elif y:
            pass
        else:
            pass
    elif b:
        if z:
            pass
    else:
        pass
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(tree.ToString());

            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_MultipleElifBranches_WorksCorrectly()
        {
            var code = @"
func test():
    if a == 1:
        return 1
    elif a == 2:
        return 2
    elif a == 3:
        return 3
    elif a == 4:
        return 4
    elif a == 5:
        return 5
    else:
        return 0
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(tree.ToString());

            var originalNodeCount = tree.AllNodes.Count();
            var clonedNodeCount = cloned.AllNodes.Count();
            clonedNodeCount.Should().Be(originalNodeCount);
        }

        [TestMethod]
        public void Clone_IfWithoutElse_WorksCorrectly()
        {
            var code = @"
func test():
    if a:
        pass
    elif b:
        pass
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            cloned.ToString().Should().Be(tree.ToString());

            var validation = GDAstValidator.Validate(cloned, cloned.ToString());
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void Clone_ElifBranchesListTokenCount_MatchesOriginal()
        {
            var code = @"
func test():
    if condition1:
        action1()
    elif condition2:
        action2()
    elif condition3:
        action3()
";
            var tree = _reader.ParseFileContent(code);

            var cloned = (GDClassDeclaration)tree.Clone();

            var originalTokenCount = tree.AllTokens.Count();
            var clonedTokenCount = cloned.AllTokens.Count();

            clonedTokenCount.Should().Be(originalTokenCount);
        }

        #endregion
    }
}
