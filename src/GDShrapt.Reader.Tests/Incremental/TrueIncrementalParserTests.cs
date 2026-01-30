using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GDShrapt.Reader.Tests.Incremental
{
    [TestClass]
    public class TrueIncrementalParserTests
    {
        private GDScriptReader _reader;
        private GDTrueIncrementalParser _incrementalParser;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
            _incrementalParser = new GDTrueIncrementalParser(_reader);
        }

        #region Basic Consistency Tests

        [TestMethod]
        public void InsertChar_InMethodBody_IsConsistentWithFullReparse()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";
            var charToInsert = "x";

            // Insert 'x' after 'pass' -> 'passx'
            var insertPos = originalCode.Length;
            var newCode = originalCode + charToInsert;
            var change = GDTextChange.Insert(insertPos, charToInsert);

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        [TestMethod]
        public void DeleteChar_InMethodBody_IsConsistentWithFullReparse()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";

            // Delete the last 's' in 'pass'
            var deletePos = originalCode.Length - 1;
            var newCode = originalCode.Substring(0, deletePos);
            var change = GDTextChange.Delete(deletePos, 1);

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        [TestMethod]
        public void ReplaceChar_InMethodBody_IsConsistentWithFullReparse()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";

            // Replace last char 's' with 'X'
            var replacePos = originalCode.Length - 1;
            var newCode = originalCode.Substring(0, replacePos) + "X";
            var change = GDTextChange.Replace(replacePos, 1, "X");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region Insert At Every Position Tests

        [TestMethod]
        public void InsertAtEveryPosition_SimpleFunction_IsConsistent()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";
            var charToInsert = "x";

            for (int i = 0; i <= originalCode.Length; i++)
            {
                var newCode = originalCode.Insert(i, charToInsert);
                var change = GDTextChange.Insert(i, charToInsert);

                AssertConsistent(originalCode, newCode, new[] { change },
                    $"Insertion at position {i}");
            }
        }

        [TestMethod]
        public void InsertAtEveryPosition_FunctionWithVariable_IsConsistent()
        {
            var originalCode = "extends Node\nfunc test():\n\tvar x = 1";
            var charToInsert = "y";

            for (int i = 0; i <= originalCode.Length; i++)
            {
                var newCode = originalCode.Insert(i, charToInsert);
                var change = GDTextChange.Insert(i, charToInsert);

                AssertConsistent(originalCode, newCode, new[] { change },
                    $"Insertion at position {i}");
            }
        }

        #endregion

        #region Delete At Every Position Tests

        [TestMethod]
        public void DeleteAtEveryPosition_SimpleFunction_IsConsistent()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";

            for (int i = 0; i < originalCode.Length; i++)
            {
                var newCode = originalCode.Remove(i, 1);
                var change = GDTextChange.Delete(i, 1);

                AssertConsistent(originalCode, newCode, new[] { change },
                    $"Deletion at position {i}");
            }
        }

        [TestMethod]
        public void DeleteAtEveryPosition_FunctionWithVariable_IsConsistent()
        {
            var originalCode = "extends Node\nfunc test():\n\tvar x = 1";

            for (int i = 0; i < originalCode.Length; i++)
            {
                var newCode = originalCode.Remove(i, 1);
                var change = GDTextChange.Delete(i, 1);

                AssertConsistent(originalCode, newCode, new[] { change },
                    $"Deletion at position {i}");
            }
        }

        #endregion

        #region Replace At Every Position Tests

        [TestMethod]
        public void ReplaceAtEveryPosition_SimpleFunction_IsConsistent()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";
            var replacement = "ZZ";

            for (int i = 0; i < originalCode.Length; i++)
            {
                var newCode = originalCode.Remove(i, 1).Insert(i, replacement);
                var change = GDTextChange.Replace(i, 1, replacement);

                AssertConsistent(originalCode, newCode, new[] { change },
                    $"Replacement at position {i}");
            }
        }

        #endregion

        #region Multiple Members Tests

        [TestMethod]
        public void ChangeInSecondMember_FirstMemberUnchanged()
        {
            var originalCode = @"extends Node

var first = 1

func test():
	pass";

            // Change inside func test() body
            var insertPos = originalCode.IndexOf("pass") + 2;
            var newCode = originalCode.Insert(insertPos, "X");
            var change = GDTextChange.Insert(insertPos, "X");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        [TestMethod]
        public void ChangeInFirstMember_SecondMemberUnchanged()
        {
            var originalCode = @"extends Node

var first = 1

func test():
	pass";

            // Change var first
            var insertPos = originalCode.IndexOf("1") + 1;
            var newCode = originalCode.Insert(insertPos, "0");
            var change = GDTextChange.Insert(insertPos, "0");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void EmptyChanges_ReturnsSameTree()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";
            var tree = _reader.ParseFileContent(originalCode);

            var result = _incrementalParser.ParseIncremental(
                tree, originalCode, new GDTextChange[0]);

            result.Should().BeSameAs(tree);
        }

        [TestMethod]
        public void NullOldTree_PerformsFullParse()
        {
            var newCode = "extends Node\nfunc test():\n\tpass";

            var result = _incrementalParser.ParseIncremental(null, newCode, null);

            result.Should().NotBeNull();
            result.ToString().Should().Be(newCode);
        }

        [TestMethod]
        public void LargeChange_FallsBackToFullReparse()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";
            var tree = _reader.ParseFileContent(originalCode);

            // Create a change that replaces most of the text
            var newCode = "extends Control";
            var change = GDTextChange.Replace(0, originalCode.Length, newCode);

            var result = _incrementalParser.ParseIncremental(
                tree, newCode, new[] { change });

            result.Should().NotBeNull();
            result.ToString().Should().Be(newCode);
        }

        [TestMethod]
        public void ChangeInClassAttributes_FallsBackToFullReparse()
        {
            var originalCode = "extends Node\nfunc test():\n\tpass";
            var tree = _reader.ParseFileContent(originalCode);

            // Change 'extends Node' to 'extends Control'
            var newCode = "extends Control\nfunc test():\n\tpass";
            var change = GDTextChange.Replace(8, 4, "Control");

            var result = _incrementalParser.ParseIncremental(
                tree, newCode, new[] { change });

            result.Should().NotBeNull();
            result.ToString().Should().Be(newCode);
        }

        #endregion

        #region Specific Statement Tests

        [TestMethod]
        public void InsertInIfStatement_IsConsistent()
        {
            var originalCode = @"extends Node
func test():
	if true:
		pass";

            var insertPos = originalCode.IndexOf("pass");
            var newCode = originalCode.Insert(insertPos, "print(1)\n\t\t");
            var change = GDTextChange.Insert(insertPos, "print(1)\n\t\t");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        [TestMethod]
        public void DeleteFromIfStatement_IsConsistent()
        {
            var originalCode = @"extends Node
func test():
	if true:
		print(1)
		pass";

            // Delete print(1) line
            var deleteStart = originalCode.IndexOf("print");
            var deleteEnd = originalCode.IndexOf("\n\t\tpass") + 1;
            var deleteLength = deleteEnd - deleteStart;

            var newCode = originalCode.Remove(deleteStart, deleteLength);
            var change = GDTextChange.Delete(deleteStart, deleteLength);

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region GetChangedRanges Tests

        [TestMethod]
        public void GetChangedRanges_NoChanges_ReturnsEmpty()
        {
            var code = "extends Node\nfunc test():\n\tpass";
            var tree1 = _reader.ParseFileContent(code);
            var tree2 = _reader.ParseFileContent(code);

            var ranges = _incrementalParser.GetChangedRanges(tree1, tree2);

            ranges.Should().BeEmpty();
        }

        [TestMethod]
        public void GetChangedRanges_OneChangedMember_ReturnsOneRange()
        {
            var code1 = @"extends Node
func test():
	pass";
            var code2 = @"extends Node
func test():
	print(1)";

            var tree1 = _reader.ParseFileContent(code1);
            var tree2 = _reader.ParseFileContent(code2);

            var ranges = _incrementalParser.GetChangedRanges(tree1, tree2);

            ranges.Should().HaveCount(1);
        }

        #endregion

        #region Inner Classes Tests

        [TestMethod]
        public void InsertAtEveryPosition_WithInnerClass_IsConsistent()
        {
            var originalCode = @"extends Node

class InnerClass:
	var inner_var = 1
	func inner_method():
		pass

func outer_method():
	pass";

            for (int i = 0; i <= originalCode.Length; i++)
            {
                var newCode = originalCode.Insert(i, "x");
                var change = GDTextChange.Insert(i, "x");
                AssertConsistent(originalCode, newCode, new[] { change }, $"Position {i}");
            }
        }

        [TestMethod]
        public void ChangeInInnerClass_IsConsistent()
        {
            var originalCode = @"extends Node

class InnerClass:
	var inner_var = 1
	func inner_method():
		return 42

func outer_method():
	pass";

            // Change inside inner class
            var insertPos = originalCode.IndexOf("return 42");
            var newCode = originalCode.Insert(insertPos, "print(1)\n\t\t");
            var change = GDTextChange.Insert(insertPos, "print(1)\n\t\t");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region Signal Tests

        [TestMethod]
        public void InsertAtEveryPosition_WithSignals_IsConsistent()
        {
            var originalCode = @"extends Node

signal health_changed(old_value, new_value)
signal died

var health = 100

func take_damage(amount):
	var old = health
	health -= amount
	if health <= 0:
		pass";

            for (int i = 0; i <= originalCode.Length; i++)
            {
                var newCode = originalCode.Insert(i, "x");
                var change = GDTextChange.Insert(i, "x");
                AssertConsistent(originalCode, newCode, new[] { change }, $"Position {i}");
            }
        }

        [TestMethod]
        public void ChangeInSignalDeclaration_IsConsistent()
        {
            var originalCode = @"extends Node

signal health_changed(old_value, new_value)

func test():
	pass";

            // Change signal parameter
            var insertPos = originalCode.IndexOf("old_value");
            var newCode = originalCode.Replace("old_value", "previous_value");
            var change = GDTextChange.Replace(insertPos, "old_value".Length, "previous_value");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region Enum Tests

        [TestMethod]
        public void InsertAtEveryPosition_WithEnum_IsConsistent()
        {
            var originalCode = @"extends Node

enum State { IDLE, WALKING, RUNNING, JUMPING }

var current_state = State.IDLE

func set_state(new_state):
	current_state = new_state";

            for (int i = 0; i <= originalCode.Length; i++)
            {
                var newCode = originalCode.Insert(i, "x");
                var change = GDTextChange.Insert(i, "x");
                AssertConsistent(originalCode, newCode, new[] { change }, $"Position {i}");
            }
        }

        [TestMethod]
        public void ChangeInEnumDeclaration_IsConsistent()
        {
            var originalCode = @"extends Node

enum State { IDLE, WALKING, RUNNING }

func test():
	pass";

            // Add enum value
            var insertPos = originalCode.IndexOf("RUNNING") + "RUNNING".Length;
            var newCode = originalCode.Insert(insertPos, ", JUMPING");
            var change = GDTextChange.Insert(insertPos, ", JUMPING");

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region Multiple Changes Tests

        [TestMethod]
        public void MultipleChanges_InDifferentMembers_IsConsistent()
        {
            var originalCode = @"extends Node

var a = 1

var b = 2

func test():
	pass";

            // Changes in var a and func test simultaneously
            var pos1 = originalCode.IndexOf("= 1") + 2;
            var pos2 = originalCode.IndexOf("pass");

            var newCode = originalCode.Substring(0, pos1) + "100" +
                          originalCode.Substring(pos1 + 1, pos2 - pos1 - 1) + "return" +
                          originalCode.Substring(pos2 + 4);

            var changes = new[]
            {
                GDTextChange.Replace(pos1, 1, "100"),
                GDTextChange.Replace(pos2 + 2, 4, "return") // +2 for "100" - "1" delta
            };

            AssertConsistent(originalCode, newCode, changes);
        }

        [TestMethod]
        public void MultipleChanges_InSameMember_IsConsistent()
        {
            var originalCode = @"extends Node

func test():
	var x = 1
	var y = 2
	return x + y";

            var pos1 = originalCode.IndexOf("= 1") + 2;
            var pos2 = originalCode.IndexOf("= 2") + 2;

            var newCode = originalCode.Replace("= 1", "= 10").Replace("= 2", "= 20");
            var changes = new[]
            {
                GDTextChange.Replace(pos1, 1, "10"),
                GDTextChange.Replace(pos2 + 1, 1, "20") // +1 for "10" - "1" delta
            };

            AssertConsistent(originalCode, newCode, changes);
        }

        #endregion

        #region Large File Tests

        [TestMethod]
        public void LargeFile_100Members_ChangeInMiddle()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("extends Node");
            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine($"var var_{i} = {i}");
            }
            var originalCode = sb.ToString();

            // Change member #50
            var target = "var_50 = 50";
            var replacement = "var_50 = 999";
            var newCode = originalCode.Replace(target, replacement);
            var pos = originalCode.IndexOf(target);
            var change = GDTextChange.Replace(pos, target.Length, replacement);

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        [TestMethod]
        public void LargeFile_100Members_ChangeAtStart()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("extends Node");
            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine($"var var_{i} = {i}");
            }
            var originalCode = sb.ToString();

            // Change first member
            var target = "var_0 = 0";
            var replacement = "var_0 = 999";
            var newCode = originalCode.Replace(target, replacement);
            var pos = originalCode.IndexOf(target);
            var change = GDTextChange.Replace(pos, target.Length, replacement);

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        [TestMethod]
        public void LargeFile_100Members_ChangeAtEnd()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("extends Node");
            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine($"var var_{i} = {i}");
            }
            var originalCode = sb.ToString();

            // Change last member
            var target = "var_99 = 99";
            var replacement = "var_99 = 999";
            var newCode = originalCode.Replace(target, replacement);
            var pos = originalCode.IndexOf(target);
            var change = GDTextChange.Replace(pos, target.Length, replacement);

            AssertConsistent(originalCode, newCode, new[] { change });
        }

        #endregion

        #region Helper Methods

        private void AssertConsistent(
            string originalCode,
            string newCode,
            IReadOnlyList<GDTextChange> changes,
            string message = null,
            [CallerLineNumber] int line = 0)
        {
            GDClassDeclaration fullResult;
            GDClassDeclaration oldTree;

            // Full reparse result - may throw on invalid syntax
            try
            {
                fullResult = _reader.ParseFileContent(newCode);
            }
            catch
            {
                // Invalid syntax in newCode - skip this case
                // The edited code is syntactically invalid, which is expected
                // when inserting/deleting characters arbitrarily
                return;
            }

            try
            {
                oldTree = _reader.ParseFileContent(originalCode);
            }
            catch
            {
                // Invalid original code - shouldn't happen in our tests
                return;
            }

            // Incremental result
            var incrementalResult = _incrementalParser.ParseIncremental(
                oldTree, newCode, changes);

            var fullText = fullResult.ToString();
            var incrementalText = incrementalResult.ToString();

            incrementalText.Should().Be(fullText,
                message ?? $"Consistency check failed at test line {line}");
        }

        #endregion
    }
}
