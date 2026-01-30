using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Incremental
{
    /// <summary>
    /// Roundtrip tests verify that:
    /// original → edit → parse → reverse_edit → parse → compare with original
    /// Specifically for GDScriptIncrementalReader.
    /// </summary>
    [TestClass]
    public class TrueIncrementalRoundtripTests
    {
        private GDScriptReader _reader;
        private GDScriptIncrementalReader _incrementalParser;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
            _incrementalParser = new GDScriptIncrementalReader(_reader);
        }

        /// <summary>
        /// Normalizes line endings to \n (GDScript standard).
        /// </summary>
        private static string N(string s) => s.Replace("\r\n", "\n");

        #region Basic Roundtrip Tests

        [TestMethod]
        public void Roundtrip_InsertThenDelete_RestoresOriginal()
        {
            var original = "extends Node\nvar x = 1";
            var insertedText = "NEW";
            var insertPosition = original.IndexOf("x") + 1; // Insert after 'x'

            // Step 1: Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Step 2: Insert text
            var insertChange = GDTextChange.Insert(insertPosition, insertedText);
            var edited = insertChange.Apply(original);

            // Step 3: Parse edited
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { insertChange });
            result2.Tree.ToString().Should().Be(edited);

            // Step 4: Delete the inserted text (reverse)
            var deleteChange = GDTextChange.Delete(insertPosition, insertedText.Length);
            var restored = deleteChange.Apply(edited);
            restored.Should().Be(original);

            // Step 5: Parse restored
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, restored, new[] { deleteChange });

            // Step 6: Compare with original
            result3.Tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_ReplaceThenReverse_RestoresOriginal()
        {
            var original = "extends Node\nvar x = 100";
            var replacePosition = original.IndexOf("100");
            var oldValue = "100";
            var newValue = "999";

            // Step 1: Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Step 2: Replace value
            var replaceChange = GDTextChange.Replace(replacePosition, oldValue.Length, newValue);
            var edited = replaceChange.Apply(original);
            edited.Should().Be("extends Node\nvar x = 999");

            // Step 3: Parse edited with incremental
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { replaceChange });
            result2.Tree.ToString().Should().Be(edited);

            // Step 4: Reverse the replacement
            var reverseChange = GDTextChange.Replace(replacePosition, newValue.Length, oldValue);
            var restored = reverseChange.Apply(edited);
            restored.Should().Be(original);

            // Step 5: Parse restored
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, restored, new[] { reverseChange });

            // Step 6: Compare
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Sequential Edits Tests

        [TestMethod]
        public void Roundtrip_SequentialEdits_PreservesConsistency()
        {
            var code = "extends Node\nvar x = 1";
            var tree = _reader.ParseFileContent(code);

            // Sequence of value changes
            var values = new[] { "1", "10", "100", "1000", "1" };

            for (int i = 0; i < values.Length - 1; i++)
            {
                var oldVal = values[i];
                var newVal = values[i + 1];
                var pos = code.LastIndexOf(oldVal);
                var change = GDTextChange.Replace(pos, oldVal.Length, newVal);
                code = code.Substring(0, pos) + newVal + code.Substring(pos + oldVal.Length);
                var result = _incrementalParser.ParseIncremental(tree, code, new[] { change });
                tree = result.Tree;

                tree.ToString().Should().Be(code, $"After change {i}: {oldVal} -> {newVal}");
            }
        }

        [TestMethod]
        public void Roundtrip_MultipleSequentialEdits_InMethodBody()
        {
            var code = N(@"extends Node
func test():
	var x = 1
	return x");

            var tree = _reader.ParseFileContent(code);

            // Series of edits to method body
            var edits = new[]
            {
                ("= 1", "= 10"),
                ("= 10", "= 100"),
                ("= 100", "= 1"),
            };

            foreach (var (oldText, newText) in edits)
            {
                var pos = code.IndexOf(oldText);
                var change = GDTextChange.Replace(pos, oldText.Length, newText);
                code = code.Replace(oldText, newText);
                var result = _incrementalParser.ParseIncremental(tree, code, new[] { change });
                tree = result.Tree;

                tree.ToString().Should().Be(code);
            }
        }

        #endregion

        #region Member Addition/Removal Tests

        [TestMethod]
        public void Roundtrip_AddRemoveMember_WorksCorrectly()
        {
            var original = "extends Node\nvar x = 1\n";
            var withMethod = "extends Node\nvar x = 1\nfunc test():\n\tpass\n";

            var tree1 = _reader.ParseFileContent(original);

            // Add method
            var addChange = GDTextChange.Insert(original.Length, "func test():\n\tpass\n");
            var result2 = _incrementalParser.ParseIncremental(tree1, withMethod, new[] { addChange });
            result2.Tree.ToString().Should().Be(withMethod);

            // Remove method
            var removeChange = GDTextChange.Delete(original.Length, withMethod.Length - original.Length);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { removeChange });
            result3.Tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_AddRemoveVariable_WorksCorrectly()
        {
            var original = N(@"extends Node
func test():
	pass
");
            var withVar = N(@"extends Node
var new_var = 42
func test():
	pass
");

            var tree1 = _reader.ParseFileContent(original);

            // Add variable after extends
            var insertPos = original.IndexOf("\nfunc");
            var addChange = GDTextChange.Insert(insertPos, "\nvar new_var = 42");
            var result2 = _incrementalParser.ParseIncremental(tree1, withVar, new[] { addChange });
            result2.Tree.ToString().Should().Be(withVar);

            // Remove variable
            var removePos = withVar.IndexOf("\nvar new_var");
            var removeLength = "\nvar new_var = 42".Length;
            var removeChange = GDTextChange.Delete(removePos, removeLength);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { removeChange });
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Method Body Editing Tests

        [TestMethod]
        public void Roundtrip_EditMethodBody_PreservesOtherMethods()
        {
            var original = N(@"extends Node

func a():
	pass

func b():
	pass

func c():
	pass
");
            var edited = N(@"extends Node

func a():
	pass

func b():
	return 42

func c():
	pass
");

            // Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Edit func b
            var passPos = original.IndexOf("pass", original.IndexOf("func b"));
            var change = GDTextChange.Replace(passPos, 4, "return 42");
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            // Reverse edit
            var returnPos = edited.IndexOf("return 42");
            var reverseChange = GDTextChange.Replace(returnPos, 9, "pass");
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Complex Structure Tests

        [TestMethod]
        public void Roundtrip_EditInsideIfStatement_WorksCorrectly()
        {
            var original = N(@"extends Node
func test():
	if condition:
		old_value = 1
	else:
		pass
");
            var edited = N(@"extends Node
func test():
	if condition:
		new_value = 100
	else:
		pass
");

            var tree1 = _reader.ParseFileContent(original);

            var oldText = "old_value = 1";
            var newText = "new_value = 100";
            var pos = original.IndexOf(oldText);
            var change = GDTextChange.Replace(pos, oldText.Length, newText);
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            // Reverse
            var reversePos = edited.IndexOf(newText);
            var reverseChange = GDTextChange.Replace(reversePos, newText.Length, oldText);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_EditComment_PreservesStructure()
        {
            var original = N(@"extends Node

# Original comment
var x = 1
");
            var edited = N(@"extends Node

# Modified comment with more text
var x = 1
");

            var tree1 = _reader.ParseFileContent(original);

            var oldComment = "# Original comment";
            var newComment = "# Modified comment with more text";
            var pos = original.IndexOf(oldComment);
            var change = GDTextChange.Replace(pos, oldComment.Length, newComment);
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            // Reverse
            var reversePos = edited.IndexOf(newComment);
            var reverseChange = GDTextChange.Replace(reversePos, newComment.Length, oldComment);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Roundtrip_EditAtStart_WorksCorrectly()
        {
            var original = "var x = 1";
            var edited = "const x = 1";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(0, 3, "const");
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            var reverseChange = GDTextChange.Replace(0, 5, "var");
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_EditAtEnd_WorksCorrectly()
        {
            var original = "var x = 1";
            var edited = "var x = 100";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(original.Length - 1, 1, "100");
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            var reverseChange = GDTextChange.Replace(edited.Length - 3, 3, "1");
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_EmptyChange_NoChange()
        {
            var original = "var x = 1";

            var tree1 = _reader.ParseFileContent(original);

            // Insert empty string
            var insertChange = GDTextChange.Insert(4, "");
            var result2 = _incrementalParser.ParseIncremental(tree1, original, new[] { insertChange });
            result2.Tree.ToString().Should().Be(original);

            // Delete nothing
            var deleteChange = GDTextChange.Delete(4, 0);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { deleteChange });
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Inner Class Roundtrip Tests

        [TestMethod]
        public void Roundtrip_EditInInnerClass_WorksCorrectly()
        {
            var original = N(@"extends Node

class Inner:
	var inner_val = 1
	func inner_method():
		pass

func outer():
	pass
");
            var edited = N(@"extends Node

class Inner:
	var inner_val = 999
	func inner_method():
		pass

func outer():
	pass
");

            var tree1 = _reader.ParseFileContent(original);

            var pos = original.IndexOf("= 1");
            var change = GDTextChange.Replace(pos + 2, 1, "999");
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            // Reverse
            var reversePos = edited.IndexOf("= 999");
            var reverseChange = GDTextChange.Replace(reversePos + 2, 3, "1");
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Signal/Enum Roundtrip Tests

        [TestMethod]
        public void Roundtrip_EditSignal_WorksCorrectly()
        {
            var original = N(@"extends Node

signal health_changed(value)

func test():
	pass
");
            var edited = N(@"extends Node

signal health_changed(old_value, new_value)

func test():
	pass
");

            var tree1 = _reader.ParseFileContent(original);

            var oldSig = "health_changed(value)";
            var newSig = "health_changed(old_value, new_value)";
            var pos = original.IndexOf(oldSig);
            var change = GDTextChange.Replace(pos, oldSig.Length, newSig);
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            // Reverse
            var reversePos = edited.IndexOf(newSig);
            var reverseChange = GDTextChange.Replace(reversePos, newSig.Length, oldSig);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_EditEnum_WorksCorrectly()
        {
            var original = N(@"extends Node

enum State { IDLE, RUNNING }

func test():
	pass
");
            var edited = N(@"extends Node

enum State { IDLE, WALKING, RUNNING, JUMPING }

func test():
	pass
");

            var tree1 = _reader.ParseFileContent(original);

            var oldEnum = "{ IDLE, RUNNING }";
            var newEnum = "{ IDLE, WALKING, RUNNING, JUMPING }";
            var pos = original.IndexOf(oldEnum);
            var change = GDTextChange.Replace(pos, oldEnum.Length, newEnum);
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            result2.Tree.ToString().Should().Be(edited);

            // Reverse
            var reversePos = edited.IndexOf(newEnum);
            var reverseChange = GDTextChange.Replace(reversePos, newEnum.Length, oldEnum);
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            result3.Tree.ToString().Should().Be(original);
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public void Roundtrip_AllIntermediateTreesAreValid()
        {
            var original = N(@"extends Node
func test():
	var x = 1
	return x
");
            var edited = N(@"extends Node
func test():
	var x = 999
	return x
");

            // Tree 1: Original
            var tree1 = _reader.ParseFileContent(original);
            GDAstValidator.Validate(tree1, original).IsValid.Should().BeTrue();

            // Tree 2: After edit
            var pos = original.IndexOf("= 1") + 2;
            var change = GDTextChange.Replace(pos, 1, "999");
            var result2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            GDAstValidator.Validate(result2.Tree, edited).IsValid.Should().BeTrue();

            // Tree 3: After reverse
            var reversePos = edited.IndexOf("= 999") + 2;
            var reverseChange = GDTextChange.Replace(reversePos, 3, "1");
            var result3 = _incrementalParser.ParseIncremental(result2.Tree, original, new[] { reverseChange });
            GDAstValidator.Validate(result3.Tree, original).IsValid.Should().BeTrue();
        }

        #endregion
    }
}
