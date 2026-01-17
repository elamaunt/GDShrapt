using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Incremental
{
    /// <summary>
    /// Tests for batch (multiple simultaneous) text changes in incremental parsing.
    /// These tests verify that the parser correctly handles cumulative position adjustments
    /// when multiple changes are applied at once.
    /// </summary>
    [TestClass]
    public class BatchEditTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();
        private readonly GDIncrementalParser _incrementalParser = new GDIncrementalParser();

        #region Basic Batch Edit Tests

        [TestMethod]
        public void BatchEdits_TwoChangesInSameFile_AppliedCorrectly()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            var expected = "var a = 100\nvar b = 200\nvar c = 3\n";

            // Two changes: 1 -> 100, 2 -> 200
            // Original positions:
            // "var a = 1" at 0-9, "1" at position 8
            // "var b = 2" at 10-19, "2" at position 18
            var changes = new[]
            {
                GDTextChange.Replace(8, 1, "100"),  // "1" -> "100"
                GDTextChange.Replace(18, 1, "200") // "2" -> "200" (in original text)
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);

            var validation = GDAstValidator.Validate(tree2, expected);
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void BatchEdits_InsertAndDelete_WorksCorrectly()
        {
            var original = "var x = 1\nvar y = 2\n";
            var expected = "var x = 1\nvar z = 3\n"; // y -> z, 2 -> 3

            var changes = new[]
            {
                GDTextChange.Replace(14, 1, "z"), // "y" -> "z" at position 14
                GDTextChange.Replace(18, 1, "3") // "2" -> "3" at position 18
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);
        }

        [TestMethod]
        public void BatchEdits_ChangesInDifferentMethods_IndependentReparse()
        {
            // Use explicit \n to avoid line ending issues
            var original = "\nfunc a():\n    return 1\n\nfunc b():\n    return 2\n";
            var expected = "\nfunc a():\n    return 100\n\nfunc b():\n    return 200\n";

            // Find positions of "1" and "2" in return statements
            var pos1 = original.IndexOf("return 1") + 7;
            var pos2 = original.IndexOf("return 2") + 7;

            var changes = new[]
            {
                GDTextChange.Replace(pos1, 1, "100"),
                GDTextChange.Replace(pos2, 1, "200")
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);

            // Verify both methods are present and correct
            tree2.Methods.Count().Should().Be(2);
        }

        #endregion

        #region Length-Changing Edits Tests

        [TestMethod]
        public void BatchEdits_LengthChanging_PositionsAdjustedCorrectly()
        {
            // First change increases length, second must account for shift
            var original = "var a = 1\nvar b = 2\n";
            var expected = "var a = 12345\nvar b = 2\n"; // 1 -> 12345 (+4 chars)

            var changes = new[]
            {
                GDTextChange.Replace(8, 1, "12345") // "1" -> "12345"
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);

            // Second var should be correctly parsed
            tree2.Members.Count().Should().Be(2);
        }

        [TestMethod]
        public void BatchEdits_MultipleLengthChanges_CumulativeDelta()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            // Change 1: "1" -> "111" (+2)
            // Change 2: "2" -> "2222" (+3)
            // Change 3: "3" -> "33333" (+4)
            var expected = "var a = 111\nvar b = 2222\nvar c = 33333\n";

            var changes = new[]
            {
                GDTextChange.Replace(8, 1, "111"),    // pos 8
                GDTextChange.Replace(18, 1, "2222"),  // pos 18 in original
                GDTextChange.Replace(28, 1, "33333") // pos 28 in original
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);
        }

        [TestMethod]
        public void BatchEdits_DeletionsThenInsertions_WorksCorrectly()
        {
            var original = "var very_long_name = 1\nvar x = 2\n";
            var expected = "var a = 1\nvar x = 2\n"; // very_long_name -> a

            var changes = new[]
            {
                GDTextChange.Replace(4, 14, "a") // "very_long_name" -> "a" (-13 chars)
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);
        }

        #endregion

        #region Overlapping Changes Tests

        [TestMethod]
        public void BatchEdits_OverlappingChangesInSameMethod_MergedCorrectly()
        {
            // Two changes in the same method body
            var original = "func test():\n    var x = 1\n    var y = 2\n";
            var expected = "func test():\n    var x = 100\n    var y = 200\n";

            var posX = original.IndexOf("= 1") + 2;
            var posY = original.IndexOf("= 2") + 2;

            var changes = new[]
            {
                GDTextChange.Replace(posX, 1, "100"),
                GDTextChange.Replace(posY, 1, "200")
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void BatchEdits_EmptyChanges_ReturnsClone()
        {
            var code = "var x = 1";
            var tree1 = _reader.ParseFileContent(code);

            var tree2 = _incrementalParser.ParseIncremental(tree1, code, System.Array.Empty<GDTextChange>());

            tree2.ToString().Should().Be(code);
            tree2.Should().NotBeSameAs(tree1); // Should be a clone
        }

        [TestMethod]
        public void BatchEdits_SingleChange_WorksLikeBefore()
        {
            var original = "var x = 1";
            var expected = "var x = 100";

            var changes = new[] { GDTextChange.Replace(8, 1, "100") };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);
        }

        [TestMethod]
        public void BatchEdits_ChangesAtBeginningAndEnd_BothApplied()
        {
            var original = "var a = 1\nvar z = 9";
            var expected = "var first = 1\nvar z = last";

            var changes = new[]
            {
                GDTextChange.Replace(4, 1, "first"), // "a" -> "first"
                GDTextChange.Replace(18, 1, "last")  // "9" -> "last"
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);

            tree2.ToString().Should().Be(expected);
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public void BatchEdits_ResultValidates_AgainstFreshParse()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            var expected = "var a = 100\nvar b = 200\nvar c = 300\n";

            var changes = new[]
            {
                GDTextChange.Replace(8, 1, "100"),
                GDTextChange.Replace(18, 1, "200"),
                GDTextChange.Replace(28, 1, "300")
            };

            var tree1 = _reader.ParseFileContent(original);
            var incrementalTree = _incrementalParser.ParseIncremental(tree1, expected, changes);
            var freshTree = _reader.ParseFileContent(expected);

            // Both should produce the same text
            incrementalTree.ToString().Should().Be(freshTree.ToString());

            // Structure should be equivalent
            var differences = GDAstValidator.CompareStructure(incrementalTree, freshTree);
            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void BatchEdits_AllIntermediateStatesValid()
        {
            var original = "func test():\n    return 1\n";
            var expected = "func test():\n    return 999\n";

            var changes = new[] { GDTextChange.Replace(original.IndexOf("1"), 1, "999") };

            var tree1 = _reader.ParseFileContent(original);
            GDAstValidator.Validate(tree1, original).IsValid.Should().BeTrue();

            var tree2 = _incrementalParser.ParseIncremental(tree1, expected, changes);
            GDAstValidator.Validate(tree2, expected).IsValid.Should().BeTrue();
        }

        #endregion
    }
}
