using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Reader.Tests.Syntax
{
    /// <summary>
    /// Tests for AST Freeze mechanism (thread-safe read-only mode).
    /// </summary>
    [TestClass]
    public class FreezeTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Basic Freeze Tests

        [TestMethod]
        public void Freeze_SetsIsFrozenFlag()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            tree.IsFrozen.Should().BeFalse();
            tree.Freeze();
            tree.IsFrozen.Should().BeTrue();
        }

        [TestMethod]
        public void Freeze_IsIdempotent()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            tree.Freeze();
            tree.Freeze(); // Second call should not throw
            tree.IsFrozen.Should().BeTrue();
        }

        [TestMethod]
        public void Freeze_FreezesAllChildNodes()
        {
            var code = @"
func test():
    var x = 1
    return x
";
            var tree = _reader.ParseFileContent(code);

            tree.Freeze();

            // All nodes should be frozen
            foreach (var node in tree.AllNodes)
            {
                node.IsFrozen.Should().BeTrue(
                    $"Node {node.TypeName} should be frozen");
            }
        }

        #endregion

        #region Modification Prevention Tests

        [TestMethod]
        public void Freeze_PreventsMemberRemoval()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var member = tree.Members.First();

            Action act = () => member.RemoveFromParent();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*frozen*");
        }

        [TestMethod]
        public void Freeze_PreventsTokenAddition()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var variable = tree.Members.OfType<GDVariableDeclaration>().First();

            Action act = () => variable.Identifier = new GDIdentifier();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*frozen*");
        }

        [TestMethod]
        public void Freeze_AllowsReadOperations()
        {
            var code = @"
func test():
    var x = 1
    return x
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            // All read operations should work without exceptions
            var text = tree.ToString();
            var tokens = tree.AllTokens.ToList();
            var nodes = tree.AllNodes.ToList();
            var methods = tree.Methods.ToList();

            text.Should().Contain("func test()");
            tokens.Should().NotBeEmpty();
            nodes.Should().NotBeEmpty();
            methods.Should().HaveCount(1);
        }

        [TestMethod]
        public void Freeze_AllowsWalkIn()
        {
            var code = @"
func test():
    var x = 1
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var visitor = new CountingVisitor();

            // WalkIn should work on frozen AST
            Action act = () => tree.WalkIn(visitor);
            act.Should().NotThrow();

            visitor.NodeCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region Clone Integration Tests

        [TestMethod]
        public void Clone_CreatesUnfrozenCopy()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var clone = (GDClassDeclaration)tree.Clone();

            tree.IsFrozen.Should().BeTrue();
            clone.IsFrozen.Should().BeFalse();
        }

        [TestMethod]
        public void Clone_AllowsModificationOfUnfrozenCopy()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var clone = (GDClassDeclaration)tree.Clone();
            var variable = clone.Members.OfType<GDVariableDeclaration>().First();

            // Modification of clone should work (not throw on unfrozen)
            Action act = () => variable.Identifier = new GDIdentifier();
            act.Should().NotThrow();

            // Verify the clone was modified
            variable.Identifier.Should().NotBeNull();
            variable.Identifier.Sequence.Should().BeNullOrEmpty();

            // Original should remain unchanged
            var originalVar = tree.Members.OfType<GDVariableDeclaration>().First();
            originalVar.Identifier.Sequence.Should().Be("x");
        }

        [TestMethod]
        public void Clone_CanFreezeClonedCopy()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var clone = (GDClassDeclaration)tree.Clone();
            clone.Freeze();

            clone.IsFrozen.Should().BeTrue();
        }

        #endregion

        #region Concurrent Access Tests

        [TestMethod]
        public void Freeze_AllowsConcurrentWalkIn()
        {
            var code = @"
class_name Test
var x = 1
var y = 2
func foo():
    return x + y
func bar():
    return x * y
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Run multiple WalkIn operations in parallel
            Parallel.For(0, 100, i =>
            {
                try
                {
                    var visitor = new CountingVisitor();
                    tree.WalkIn(visitor);

                    // Verify consistent results
                    if (visitor.NodeCount < 5)
                    {
                        throw new Exception($"Unexpected node count: {visitor.NodeCount}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty(
                $"Concurrent WalkIn should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");
        }

        [TestMethod]
        public void Freeze_AllowsConcurrentTokenIteration()
        {
            var code = @"
var a = 1
var b = 2
var c = 3
func test():
    return a + b + c
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var expectedTokenCount = tree.AllTokens.Count();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var tokens = tree.AllTokens.ToList();
                    if (tokens.Count != expectedTokenCount)
                    {
                        throw new Exception($"Token count mismatch: expected {expectedTokenCount}, got {tokens.Count}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty();
        }

        [TestMethod]
        public void Freeze_AllowsConcurrentNodeIteration()
        {
            var code = @"
func test():
    if true:
        for i in range(10):
            while x:
                pass
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var expectedNodeCount = tree.AllNodes.Count();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var nodes = tree.AllNodes.ToList();
                    if (nodes.Count != expectedNodeCount)
                    {
                        throw new Exception($"Node count mismatch: expected {expectedNodeCount}, got {nodes.Count}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty();
        }

        #endregion

        #region Snapshot Tests

        [TestMethod]
        public void Freeze_DirectReturnsSnapshot()
        {
            var code = "var x = 1\nvar y = 2";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            // Multiple calls should return same content
            var tokens1 = tree.Tokens.ToList();
            var tokens2 = tree.Tokens.ToList();

            tokens1.Should().BeEquivalentTo(tokens2);
        }

        [TestMethod]
        public void Freeze_SnapshotMatchesOriginalContent()
        {
            var code = @"
func test():
    var x = 1
    return x
";
            var tree = _reader.ParseFileContent(code);

            var tokensBeforeFreeze = tree.AllTokens.ToList();
            tree.Freeze();
            var tokensAfterFreeze = tree.AllTokens.ToList();

            tokensAfterFreeze.Should().HaveCount(tokensBeforeFreeze.Count);
            // Normalize line endings for comparison (parser may strip \r)
            tree.ToString().Replace("\r\n", "\n").Should().Be(code.Replace("\r\n", "\n"));
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Freeze_EmptyClass_WorksCorrectly()
        {
            var code = "";
            var tree = _reader.ParseFileContent(code);

            tree.Freeze();

            tree.IsFrozen.Should().BeTrue();
            tree.ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void Freeze_DeepNesting_FreezesAllLevels()
        {
            var code = @"
func outer():
    if true:
        for i in range(10):
            while x:
                match y:
                    1:
                        pass
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            // Find the deepest node (pass expression)
            var deepestNodes = tree.AllNodes
                .Where(n => n is GDPassExpression)
                .ToList();

            deepestNodes.Should().NotBeEmpty();
            foreach (var node in deepestNodes)
            {
                node.IsFrozen.Should().BeTrue();
            }
        }

        [TestMethod]
        public void Freeze_InnerClasses_FreezesRecursively()
        {
            var code = @"
class_name Outer

class Inner:
    var x = 1

    class DeepInner:
        var y = 2
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var innerClasses = tree.AllNodes
                .OfType<GDInnerClassDeclaration>()
                .ToList();

            innerClasses.Should().HaveCount(2);
            foreach (var innerClass in innerClasses)
            {
                innerClass.IsFrozen.Should().BeTrue();
            }
        }

        #endregion

        #region Thread Safety for LinkedList Methods

        [TestMethod]
        public void Freeze_FirstToken_UsesSnapshot()
        {
            var code = "var x = 1\nvar y = 2";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var first = tree.Form.FirstToken;
                    var last = tree.Form.LastToken;
                    first.Should().NotBeNull();
                    last.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty(
                $"Concurrent access to FirstToken/LastToken should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");
        }

        [TestMethod]
        public void Freeze_NextTokenAfter_UsesSnapshot()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            // Get a variable declaration and use its form which has multiple tokens
            var varDecl = tree.Members.OfType<GDVariableDeclaration>().First();
            var firstToken = varDecl.Form.FirstToken;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var next = varDecl.Form.NextTokenAfter(firstToken);
                    next.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty(
                $"Concurrent access to NextTokenAfter should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");
        }

        [TestMethod]
        public void Freeze_GetAllTokensAfter_UsesSnapshot()
        {
            var code = "var x = 1\nvar y = 2";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var firstToken = tree.Form.FirstToken;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var counts = new System.Collections.Concurrent.ConcurrentBag<int>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var tokens = tree.Form.GetAllTokensAfter(firstToken).ToList();
                    counts.Add(tokens.Count);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty(
                $"Concurrent access to GetAllTokensAfter should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");

            // All threads should see the same count
            counts.Distinct().Should().HaveCount(1, "All concurrent GetAllTokensAfter calls should return consistent results");
        }

        [TestMethod]
        public void Freeze_FindFirst_UsesSnapshot()
        {
            var code = @"
func test():
    var x = 1
    return x
";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var firstNode = tree.Form.FirstNode;
                    var lastNode = tree.Form.LastNode;
                    firstNode.Should().NotBeNull();
                    lastNode.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty(
                $"Concurrent access to FirstNode/LastNode should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");
        }

        [TestMethod]
        public void Freeze_Contains_UsesSnapshot()
        {
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);
            tree.Freeze();

            var firstToken = tree.Form.FirstToken;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var results = new System.Collections.Concurrent.ConcurrentBag<bool>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    var contains = tree.Form.Contains(firstToken);
                    results.Add(contains);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty();
            results.Should().AllBeEquivalentTo(true);
        }

        #endregion

        #region Helper Classes

        private class CountingVisitor : GDVisitor
        {
            public int NodeCount { get; private set; }

            public override void WillVisit(GDNode node) => NodeCount++;
        }

        #endregion
    }
}
