using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace GDShrapt.Reader.Tests.Incremental
{
    /// <summary>
    /// Roundtrip tests verify that:
    /// original → edit → parse → reverse_edit → parse → compare with original
    /// </summary>
    [TestClass]
    public class RoundtripEditTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();
        private readonly GDIncrementalParser _incrementalParser = new GDIncrementalParser();

        #region Basic Roundtrip Tests

        [TestMethod]
        public void Roundtrip_InsertThenDelete_RestoresOriginal()
        {
            var original = "var x = 1";
            var insertedText = "NEW";
            var insertPosition = 4; // Insert after "var "

            // Step 1: Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Step 2: Insert text
            var insertChange = GDTextChange.Insert(insertPosition, insertedText);
            var edited = insertChange.Apply(original);
            edited.Should().Be("var NEWx = 1");

            // Step 3: Parse edited
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { insertChange });

            // Step 4: Delete the inserted text (reverse)
            var deleteChange = GDTextChange.Delete(insertPosition, insertedText.Length);
            var restored = deleteChange.Apply(edited);
            restored.Should().Be(original);

            // Step 5: Parse restored
            var tree3 = _incrementalParser.ParseIncremental(tree2, restored, new[] { deleteChange });

            // Step 6: Compare with original
            tree3.ToString().Should().Be(original);

            var differences = GDAstValidator.CompareStructure(tree1, tree3);
            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void Roundtrip_ReplaceThenReverse_RestoresOriginal()
        {
            var original = "var x = 100";
            var replacePosition = 8;
            var oldValue = "100";
            var newValue = "999";

            // Step 1: Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Step 2: Replace value
            var replaceChange = GDTextChange.Replace(replacePosition, oldValue.Length, newValue);
            var edited = replaceChange.Apply(original);
            edited.Should().Be("var x = 999");

            // Step 3: Parse edited with incremental
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { replaceChange });

            // Step 4: Reverse the replacement
            var reverseChange = GDTextChange.Replace(replacePosition, newValue.Length, oldValue);
            var restored = reverseChange.Apply(edited);
            restored.Should().Be(original);

            // Step 5: Parse restored
            var tree3 = _incrementalParser.ParseIncremental(tree2, restored, new[] { reverseChange });

            // Step 6: Compare
            tree3.ToString().Should().Be(original);
        }

        #endregion

        #region Multiple Edits Roundtrip Tests

        [TestMethod]
        public void Roundtrip_MultipleSequentialEdits_Reversible()
        {
            var original = "var x = 1";

            // Parse original
            var tree = _reader.ParseFileContent(original);

            // Sequence of edits: 1 -> 2 -> 3 -> 10 -> 1
            var values = new[] { "1", "2", "3", "10", "1" };
            var currentText = original;

            for (int i = 0; i < values.Length - 1; i++)
            {
                var oldValue = values[i];
                var newValue = values[i + 1];
                var pos = currentText.IndexOf(oldValue, 8); // Start after "var x = "

                var change = GDTextChange.Replace(pos, oldValue.Length, newValue);
                var newText = change.Apply(currentText);

                tree = _incrementalParser.ParseIncremental(tree, newText, new[] { change });
                tree.ToString().Should().Be(newText);

                currentText = newText;
            }

            // After all edits, should be back to original value
            tree.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_BatchEditsAndReverse_RestoresOriginal()
        {
            // Now with real batch edits - multiple changes applied at once
            var original = "var a = 1\nvar b = 2\nvar c = 3\n";
            var edited = "var a = 100\nvar b = 200\nvar c = 3\n";

            // Forward: two changes in original positions
            var forwardChanges = new[]
            {
                GDTextChange.Replace(8, 1, "100"),  // "1" -> "100"
                GDTextChange.Replace(18, 1, "200") // "2" -> "200"
            };

            var tree1 = _reader.ParseFileContent(original);
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, forwardChanges);

            tree2.ToString().Should().Be(edited);

            // Reverse: restore original values
            // After forward edit:
            // "var a = 100\n" (12 chars) + "var b = 200\n" (12 chars) + "var c = 3\n"
            // "100" is at position 8, "200" is at position 8+12=20
            var reverseChanges = new[]
            {
                GDTextChange.Replace(8, 3, "1"),   // "100" -> "1" at pos 8
                GDTextChange.Replace(20, 3, "2")  // "200" -> "2" at pos 20 (in edited text)
            };

            var tree3 = _incrementalParser.ParseIncremental(tree2, original, reverseChanges);

            tree3.ToString().Should().Be(original);
        }

        #endregion

        #region Method Editing Roundtrip Tests

        [TestMethod]
        public void Roundtrip_EditMethodBody_PreservesOtherMethods()
        {
            var original = "\nfunc a():\n    pass\n\nfunc b():\n    pass\n\nfunc c():\n    pass\n";
            var edited = "\nfunc a():\n    pass\n\nfunc b():\n    return 42\n\nfunc c():\n    pass\n";

            // Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Edit func b
            var change = GDTextChange.Replace(
                original.IndexOf("pass", original.IndexOf("func b")),
                4,
                "return 42");

            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });

            tree2.ToString().Should().Be(edited);

            // Reverse edit
            var reverseChange = GDTextChange.Replace(
                edited.IndexOf("return 42"),
                9, // Length of "return 42"
                "pass");

            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });

            tree3.ToString().Should().Be(original);

            // Verify func a and func c are preserved
            var differences = GDAstValidator.CompareStructure(tree1, tree3);
            differences.Should().BeEmpty();
        }

        [TestMethod]
        public void Roundtrip_AddRemoveMethod_WorksCorrectly()
        {
            var original = "func existing():\n    pass\n";
            var withNewMethod = "func existing():\n    pass\n\nfunc new_method():\n    return 1\n";

            // Parse original
            var tree1 = _reader.ParseFileContent(original);

            // Add new method
            var addChange = GDTextChange.Insert(original.Length, "\nfunc new_method():\n    return 1\n");
            var tree2 = _incrementalParser.ParseIncremental(tree1, withNewMethod, new[] { addChange });

            tree2.ToString().Should().Be(withNewMethod);

            // Remove new method (delete from original length to end)
            var removeChange = GDTextChange.Delete(original.Length, withNewMethod.Length - original.Length);
            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { removeChange });

            tree3.ToString().Should().Be(original);
        }

        #endregion

        #region Complex Structure Roundtrip Tests

        [TestMethod]
        public void Roundtrip_EditInsideIfStatement_WorksCorrectly()
        {
            var original = "\nfunc test():\n    if condition:\n        old_value = 1\n    else:\n        pass\n";
            var edited = "\nfunc test():\n    if condition:\n        new_value = 100\n    else:\n        pass\n";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(
                original.IndexOf("old_value = 1"),
                "old_value = 1".Length,
                "new_value = 100");

            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            tree2.ToString().Should().Be(edited);

            // Reverse
            var reverseChange = GDTextChange.Replace(
                edited.IndexOf("new_value = 100"),
                "new_value = 100".Length,
                "old_value = 1");

            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });
            tree3.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_EditComment_PreservesStructure()
        {
            var original = "\n# Original comment\nvar x = 1\n";
            var edited = "\n# Modified comment with more text\nvar x = 1\n";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(
                original.IndexOf("# Original comment"),
                "# Original comment".Length,
                "# Modified comment with more text");

            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            tree2.ToString().Should().Be(edited);

            // Reverse
            var reverseChange = GDTextChange.Replace(
                edited.IndexOf("# Modified comment with more text"),
                "# Modified comment with more text".Length,
                "# Original comment");

            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });
            tree3.ToString().Should().Be(original);
        }

        #endregion

        #region Edge Case Roundtrip Tests

        [TestMethod]
        public void Roundtrip_EditAtStart_WorksCorrectly()
        {
            var original = "var x = 1";
            var edited = "const x = 1";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(0, 3, "const");
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            tree2.ToString().Should().Be(edited);

            var reverseChange = GDTextChange.Replace(0, 5, "var");
            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });
            tree3.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_EditAtEnd_WorksCorrectly()
        {
            var original = "var x = 1";
            var edited = "var x = 100";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(original.Length - 1, 1, "100");
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            tree2.ToString().Should().Be(edited);

            var reverseChange = GDTextChange.Replace(edited.Length - 3, 3, "1");
            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });
            tree3.ToString().Should().Be(original);
        }

        [TestMethod]
        public void Roundtrip_InsertAndDeleteEmpty_NoChange()
        {
            var original = "var x = 1";

            var tree1 = _reader.ParseFileContent(original);

            // Insert empty string
            var insertChange = GDTextChange.Insert(4, "");
            var tree2 = _incrementalParser.ParseIncremental(tree1, original, new[] { insertChange });
            tree2.ToString().Should().Be(original);

            // Delete nothing
            var deleteChange = GDTextChange.Delete(4, 0);
            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { deleteChange });
            tree3.ToString().Should().Be(original);
        }

        #endregion

        #region Validation After Roundtrip Tests

        [TestMethod]
        public void Roundtrip_AllIntermediateTreesAreValid()
        {
            var original = "\nfunc test():\n    var x = 1\n    return x\n";
            var edited = "\nfunc test():\n    var x = 999\n    return x\n";

            // Tree 1: Original
            var tree1 = _reader.ParseFileContent(original);
            GDAstValidator.Validate(tree1, original).IsValid.Should().BeTrue();

            // Tree 2: After edit
            var change = GDTextChange.Replace(original.IndexOf("= 1") + 2, 1, "999");
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });
            GDAstValidator.Validate(tree2, edited).IsValid.Should().BeTrue();

            // Tree 3: After reverse
            var reverseChange = GDTextChange.Replace(edited.IndexOf("= 999") + 2, 3, "1");
            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });
            GDAstValidator.Validate(tree3, original).IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Roundtrip_ResultStructurallyEqualsOriginal()
        {
            var original = "\nclass_name Test\n\nvar health = 100\n\nfunc take_damage(amount):\n    health -= amount\n    if health <= 0:\n        die()\n";
            var edited = "\nclass_name Test\n\nvar health = 200\n\nfunc take_damage(amount):\n    health -= amount\n    if health <= 0:\n        die()\n";

            var tree1 = _reader.ParseFileContent(original);

            var change = GDTextChange.Replace(original.IndexOf("100"), 3, "200");
            var tree2 = _incrementalParser.ParseIncremental(tree1, edited, new[] { change });

            var reverseChange = GDTextChange.Replace(edited.IndexOf("200"), 3, "100");
            var tree3 = _incrementalParser.ParseIncremental(tree2, original, new[] { reverseChange });

            // Structural comparison
            var differences = GDAstValidator.CompareStructure(tree1, tree3);
            differences.Should().BeEmpty();
        }

        #endregion
    }
}
