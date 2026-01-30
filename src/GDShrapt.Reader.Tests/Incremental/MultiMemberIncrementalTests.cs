using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Incremental
{
    /// <summary>
    /// Tests for multi-member incremental parsing.
    /// Verifies that changes affecting 2-3 members are handled incrementally.
    /// </summary>
    [TestClass]
    public class MultiMemberIncrementalTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();
        private readonly GDScriptIncrementalReader _incrementalReader;

        public MultiMemberIncrementalTests()
        {
            _incrementalReader = new GDScriptIncrementalReader(_reader);
        }

        #region Two Consecutive Members Tests

        [TestMethod]
        public void TwoConsecutiveMembers_BothReparsed()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            var expected = "var a = 100\nvar b = 200\nvar c = 3\n";

            // Positions in original text
            var posA = original.IndexOf("= 1") + 2; // position of "1"
            var posB = original.IndexOf("= 2") + 2; // position of "2"

            var changes = new[]
            {
                GDTextChange.Replace(posA, 1, "100"),
                GDTextChange.Replace(posB, 1, "200")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue("should be incremental");
            result.IsFullReparse.Should().BeFalse();
            result.ChangedMembers.Should().HaveCount(2);
            result.ChangedMembers[0].Index.Should().Be(0);
            result.ChangedMembers[1].Index.Should().Be(1);
            result.Tree.ToOriginalString().Should().Be(expected);

            // Validate against fresh parse
            var freshTree = _reader.ParseFileContent(expected);
            var differences = GDAstValidator.CompareStructure(result.Tree, freshTree);
            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void TwoConsecutiveMethods_BothReparsed()
        {
            var original = @"func a():
	return 1

func b():
	return 2
";
            var expected = @"func a():
	return 100

func b():
	return 200
";

            var posA = original.IndexOf("return 1") + 7; // position of "1"
            var posB = original.IndexOf("return 2") + 7; // position of "2"

            var changes = new[]
            {
                GDTextChange.Replace(posA, 1, "100"),
                GDTextChange.Replace(posB, 1, "200")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue();
            result.ChangedMembers.Should().HaveCount(2);
            result.Tree.ToOriginalString().Should().Be(expected);
        }

        #endregion

        #region Three Consecutive Members Tests

        [TestMethod]
        public void ThreeConsecutiveMembers_AllReparsed()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            var expected = "var a = 100\nvar b = 200\nvar c = 300\n";

            var posA = original.IndexOf("= 1") + 2;
            var posB = original.IndexOf("= 2") + 2;
            var posC = original.IndexOf("= 3") + 2;

            var changes = new[]
            {
                GDTextChange.Replace(posA, 1, "100"),
                GDTextChange.Replace(posB, 1, "200"),
                GDTextChange.Replace(posC, 1, "300")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue();
            result.ChangedMembers.Should().HaveCount(3);
            result.Tree.ToOriginalString().Should().Be(expected);
        }

        #endregion

        #region Non-Consecutive Members Tests

        [TestMethod]
        public void NonConsecutiveMembers_BothReparsed()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\nvar d = 4\nvar e = 5\nvar f = 6\n";
            var expected = "var a = 100\nvar b = 2\nvar c = 3\nvar d = 4\nvar e = 500\nvar f = 6\n";

            // Change members 0 and 4 (non-consecutive)
            var posA = original.IndexOf("= 1") + 2;
            var posE = original.IndexOf("= 5") + 2;

            var changes = new[]
            {
                GDTextChange.Replace(posA, 1, "100"),
                GDTextChange.Replace(posE, 1, "500")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            // Non-consecutive should still work with the new implementation
            result.IsIncremental.Should().BeTrue();
            result.ChangedMembers.Should().HaveCount(2);
            result.ChangedMembers[0].Index.Should().Be(0); // member a
            result.ChangedMembers[1].Index.Should().Be(4); // member e
            result.Tree.ToOriginalString().Should().Be(expected);
        }

        #endregion

        #region Length Changing Edits Tests

        [TestMethod]
        public void LengthChangingEdits_CumulativeOffsetTracking()
        {
            var original = "var a = 1\nvar b = 2\n";
            var expected = "var a = 12345\nvar b = 67890\n";

            var posA = original.IndexOf("= 1") + 2;
            var posB = original.IndexOf("= 2") + 2;

            var changes = new[]
            {
                GDTextChange.Replace(posA, 1, "12345"), // +4 chars
                GDTextChange.Replace(posB, 1, "67890")  // +4 chars, but position is in ORIGINAL text
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue();
            result.ChangedMembers.Should().HaveCount(2);
            result.Tree.ToOriginalString().Should().Be(expected);

            // Validate structure
            var freshTree = _reader.ParseFileContent(expected);
            var differences = GDAstValidator.CompareStructure(result.Tree, freshTree);
            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void LengthDecreasing_CumulativeOffsetTracking()
        {
            // Use longer original text to keep changes below threshold (50%)
            var original = "var very_long_variable_name_here = 1\nvar another_long_variable_name = 2\nvar unchanged = 3\nvar also_unchanged = 4\n";
            var expected = "var a = 1\nvar b = 2\nvar unchanged = 3\nvar also_unchanged = 4\n";

            var posName1 = original.IndexOf("very_long_variable_name_here");
            var posName2 = original.IndexOf("another_long_variable_name");

            var changes = new[]
            {
                GDTextChange.Replace(posName1, "very_long_variable_name_here".Length, "a"),
                GDTextChange.Replace(posName2, "another_long_variable_name".Length, "b")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue();
            result.Tree.ToOriginalString().Should().Be(expected);
        }

        #endregion

        #region Cross-Member Edit Tests

        [TestMethod]
        public void CrossMemberEdit_FallsBackToFullReparse()
        {
            var original = "var a = 1\nvar b = 2\n";
            // Delete from middle of first var to middle of second var
            var startPos = original.IndexOf("1");
            var endPos = original.IndexOf("2") + 1;
            var deleteLength = endPos - startPos;

            var expected = original.Remove(startPos, deleteLength).Insert(startPos, "X");

            var changes = new[]
            {
                GDTextChange.Replace(startPos, deleteLength, "X")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            // Cross-member edits should trigger full reparse
            result.IsFullReparse.Should().BeTrue();
        }

        #endregion

        #region Max Affected Members Tests

        [TestMethod]
        public void ExceedsMaxAffectedMembers_FallsBackToFullReparse()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\nvar d = 4\nvar e = 5\n";
            var expected = "var a = 10\nvar b = 20\nvar c = 30\nvar d = 40\nvar e = 50\n";

            // 5 changes for 5 members - exceeds default max of 3
            var changes = new[]
            {
                GDTextChange.Replace(original.IndexOf("= 1") + 2, 1, "10"),
                GDTextChange.Replace(original.IndexOf("= 2") + 2, 1, "20"),
                GDTextChange.Replace(original.IndexOf("= 3") + 2, 1, "30"),
                GDTextChange.Replace(original.IndexOf("= 4") + 2, 1, "40"),
                GDTextChange.Replace(original.IndexOf("= 5") + 2, 1, "50")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsFullReparse.Should().BeTrue("should fall back when exceeding max affected members");
        }

        [TestMethod]
        public void CustomMaxAffectedMembers_Respected()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\nvar d = 4\nvar e = 5\n";
            var expected = "var a = 10\nvar b = 20\nvar c = 30\nvar d = 40\nvar e = 50\n";

            var changes = new[]
            {
                GDTextChange.Replace(original.IndexOf("= 1") + 2, 1, "10"),
                GDTextChange.Replace(original.IndexOf("= 2") + 2, 1, "20"),
                GDTextChange.Replace(original.IndexOf("= 3") + 2, 1, "30"),
                GDTextChange.Replace(original.IndexOf("= 4") + 2, 1, "40"),
                GDTextChange.Replace(original.IndexOf("= 5") + 2, 1, "50")
            };

            var incrementalReader = new GDScriptIncrementalReader(_reader)
            {
                MaxAffectedMembersForIncremental = 5
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue("should be incremental with max=5");
            result.ChangedMembers.Should().HaveCount(5);
        }

        #endregion

        #region Single Member Tests (Backward Compatibility)

        [TestMethod]
        public void SingleMember_UsesChangedMembers()
        {
            var original = "var x = 1";
            var expected = "var x = 100";

            var changes = new[] { GDTextChange.Replace(8, 1, "100") };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            result.IsIncremental.Should().BeTrue();
            result.ChangedMembers.Should().HaveCount(1);
            result.ChangedMembers[0].Index.Should().Be(0);
            result.ChangedMembers[0].OldMember.Should().NotBeNull();
            result.ChangedMembers[0].NewMember.Should().NotBeNull();
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public void ValidationAgainstFreshParse_AllMembersCorrect()
        {
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            var expected = "var a = 100\nvar b = 200\nvar c = 3\n";

            var changes = new[]
            {
                GDTextChange.Replace(original.IndexOf("= 1") + 2, 1, "100"),
                GDTextChange.Replace(original.IndexOf("= 2") + 2, 1, "200")
            };

            var tree1 = _reader.ParseFileContent(original);
            var incrementalResult = _incrementalReader.ParseIncremental(tree1, expected, changes);
            var freshResult = _reader.ParseFileContent(expected);

            // Text should match
            incrementalResult.Tree.ToOriginalString().Should().Be(freshResult.ToOriginalString());

            // Structure should be equivalent
            var differences = GDAstValidator.CompareStructure(incrementalResult.Tree, freshResult);
            differences.Should().BeEmpty();

            // Validate AST
            var validation = GDAstValidator.Validate(incrementalResult.Tree, expected);
            validation.IsValid.Should().BeTrue(string.Join("\n", validation.Errors));
        }

        [TestMethod]
        public void StructureComparison_AfterMultiMemberChange()
        {
            var original = @"func first():
	pass

func second():
	pass
";
            var expected = @"func first():
	return 1

func second():
	return 2
";

            var posPass1 = original.IndexOf("pass");
            var posPass2 = original.LastIndexOf("pass");

            var changes = new[]
            {
                GDTextChange.Replace(posPass1, 4, "return 1"),
                GDTextChange.Replace(posPass2, 4, "return 2")
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);
            var freshTree = _reader.ParseFileContent(expected);

            var differences = GDAstValidator.CompareStructure(result.Tree, freshTree);
            differences.Should().BeEmpty();
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void EmptyChanges_ReturnsNoChanges()
        {
            var code = "var x = 1";
            var tree1 = _reader.ParseFileContent(code);

            var result = _incrementalReader.ParseIncremental(tree1, code, System.Array.Empty<GDTextChange>());

            result.Tree.Should().BeSameAs(tree1);
            result.IsFullReparse.Should().BeFalse();
            result.IsIncremental.Should().BeFalse();
        }

        [TestMethod]
        public void ClassAttributeChange_FallsBackToFullReparse()
        {
            var original = "extends Node\nvar x = 1\n";
            var expected = "extends Node2D\nvar x = 1\n";

            var changes = new[]
            {
                GDTextChange.Replace(8, 4, "Node2D") // "Node" -> "Node2D"
            };

            var tree1 = _reader.ParseFileContent(original);
            var result = _incrementalReader.ParseIncremental(tree1, expected, changes);

            // Changes to class attributes should trigger full reparse
            result.IsFullReparse.Should().BeTrue();
        }

        #endregion
    }
}
