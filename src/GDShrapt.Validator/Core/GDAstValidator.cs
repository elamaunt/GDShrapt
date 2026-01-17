using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates AST structural integrity after incremental updates or other modifications.
    /// Ensures parent-child consistency, no cycles, text equivalence, and token uniqueness.
    /// </summary>
    public static class GDAstValidator
    {
        /// <summary>
        /// Result of AST validation.
        /// </summary>
        public class ValidationResult
        {
            /// <summary>
            /// True if all validation checks passed.
            /// </summary>
            public bool IsValid { get; }

            /// <summary>
            /// List of error messages describing validation failures.
            /// </summary>
            public IReadOnlyList<string> Errors { get; }

            private ValidationResult(bool isValid, IReadOnlyList<string> errors)
            {
                IsValid = isValid;
                Errors = errors ?? Array.Empty<string>();
            }

            /// <summary>
            /// Creates a successful validation result.
            /// </summary>
            public static ValidationResult Valid() => new ValidationResult(true, Array.Empty<string>());

            /// <summary>
            /// Creates a failed validation result with error messages.
            /// </summary>
            public static ValidationResult Invalid(params string[] errors)
                => new ValidationResult(false, errors);

            /// <summary>
            /// Creates a validation result from a list of errors.
            /// </summary>
            public static ValidationResult FromErrors(List<string> errors)
                => errors.Count == 0 ? Valid() : new ValidationResult(false, errors);
        }

        /// <summary>
        /// Validates AST structure after incremental parsing or modification.
        /// </summary>
        /// <param name="tree">The AST root to validate.</param>
        /// <param name="expectedText">Optional expected text for text equivalence check.</param>
        /// <returns>Validation result with any errors found.</returns>
        public static ValidationResult Validate(GDClassDeclaration tree, string expectedText = null)
        {
            if (tree == null)
                return ValidationResult.Invalid("Tree is null");

            var errors = new List<string>();

            // 1. Parent-child consistency
            ValidateParentChildRelations(tree, null, errors);

            // 2. No circular references
            ValidateNoCycles(tree, errors);

            // 3. No orphan tokens (all tokens reachable from root)
            ValidateNoOrphans(tree, errors);

            // 4. Text equivalence (CRITICAL - catches token loss/duplication)
            if (expectedText != null)
                ValidateTextEquivalence(tree, expectedText, errors);

            // 5. Position monotonicity (no overlaps)
            ValidatePositionMonotonicity(tree, errors);

            // 6. Token uniqueness (no duplicates)
            ValidateTokenUniqueness(tree, errors);

            return ValidationResult.FromErrors(errors);
        }

        #region Validation Methods

        /// <summary>
        /// Validates that all parent-child relationships are consistent.
        /// Each token's Parent property should point to its actual parent in the tree.
        /// </summary>
        private static void ValidateParentChildRelations(GDNode node, GDNode expectedParent, List<string> errors)
        {
            // Check that node's parent matches expected
            if (expectedParent != null && node.Parent != expectedParent)
            {
                errors.Add($"Node {node.TypeName} at line {node.StartLine} has wrong parent. " +
                    $"Expected: {expectedParent.TypeName}, Actual: {node.Parent?.TypeName ?? "null"}");
            }

            // Check all direct children
            foreach (var token in node.Tokens)
            {
                if (token.Parent != node)
                {
                    errors.Add($"Token {token.TypeName} at line {token.StartLine} has wrong parent. " +
                        $"Expected: {node.TypeName}, Actual: {token.Parent?.TypeName ?? "null"}");
                }

                // Recursively validate child nodes
                if (token is GDNode childNode)
                {
                    ValidateParentChildRelations(childNode, node, errors);
                }
            }
        }

        /// <summary>
        /// Validates that there are no circular references in the tree.
        /// </summary>
        private static void ValidateNoCycles(GDNode root, List<string> errors)
        {
            var visited = new HashSet<GDNode>(ReferenceEqualityComparer<GDNode>.Instance);
            var stack = new Stack<GDNode>();

            void Visit(GDNode node)
            {
                if (!visited.Add(node))
                {
                    errors.Add($"Circular reference detected at {node.TypeName} (line {node.StartLine})");
                    return;
                }

                stack.Push(node);

                foreach (var child in node.Nodes)
                {
                    // Check if child is already in current path (cycle)
                    if (stack.Contains(child))
                    {
                        errors.Add($"Cycle detected: {node.TypeName} -> {child.TypeName}");
                        continue;
                    }
                    Visit(child);
                }

                stack.Pop();
            }

            Visit(root);
        }

        /// <summary>
        /// Validates that all tokens have proper parent references (no orphans).
        /// </summary>
        private static void ValidateNoOrphans(GDNode root, List<string> errors)
        {
            foreach (var token in root.AllTokens)
            {
                if (token.Parent == null)
                {
                    var preview = Truncate(token.ToString(), 20);
                    errors.Add($"Orphan token found: {token.TypeName} '{preview}' has null parent");
                }
            }
        }

        /// <summary>
        /// Validates that the tree's text representation matches the expected text.
        /// This is critical for detecting token loss or duplication.
        /// </summary>
        private static void ValidateTextEquivalence(GDNode tree, string expected, List<string> errors)
        {
            var actual = tree.ToString();
            if (actual != expected)
            {
                var diffPos = FindFirstDifference(expected, actual);
                var expectedSnippet = SafeSubstring(expected, diffPos, 30);
                var actualSnippet = SafeSubstring(actual, diffPos, 30);

                errors.Add($"Text mismatch at position {diffPos}. " +
                    $"Expected[{diffPos}..]: '{expectedSnippet}', " +
                    $"Actual[{diffPos}..]: '{actualSnippet}'. " +
                    $"Expected length: {expected.Length}, Actual length: {actual.Length}");
            }
        }

        /// <summary>
        /// Validates that tokens don't overlap in their positions.
        /// Tokens should appear in order without position conflicts.
        /// </summary>
        private static void ValidatePositionMonotonicity(GDNode tree, List<string> errors)
        {
            var allTokens = tree.AllTokens.ToList();
            if (allTokens.Count < 2)
                return;

            int prevEndLine = 0;
            int prevEndColumn = 0;
            GDSyntaxToken prevToken = null;

            foreach (var token in allTokens)
            {
                if (prevToken != null)
                {
                    var currStartLine = token.StartLine;
                    var currStartColumn = token.StartColumn;

                    // If same line, start column should not be before previous end column
                    if (currStartLine == prevEndLine)
                    {
                        if (currStartColumn < prevEndColumn)
                        {
                            // Allow newlines to "reset" the column
                            if (!(prevToken is GDNewLine))
                            {
                                errors.Add($"Token overlap on line {currStartLine}: " +
                                    $"{prevToken.TypeName} ends at col {prevEndColumn}, " +
                                    $"but {token.TypeName} starts at col {currStartColumn}");
                            }
                        }
                    }
                    else if (currStartLine < prevEndLine)
                    {
                        errors.Add($"Token line order violation: " +
                            $"{prevToken.TypeName} ends at line {prevEndLine}, " +
                            $"but {token.TypeName} starts at line {currStartLine}");
                    }
                }

                prevEndLine = token.EndLine;
                prevEndColumn = token.EndColumn;
                prevToken = token;
            }
        }

        /// <summary>
        /// Validates that no token instance appears twice in the tree.
        /// This catches bugs where the same token object is added to multiple parents.
        /// </summary>
        private static void ValidateTokenUniqueness(GDNode root, List<string> errors)
        {
            var seen = new HashSet<GDSyntaxToken>(ReferenceEqualityComparer<GDSyntaxToken>.Instance);

            foreach (var token in root.AllTokens)
            {
                if (!seen.Add(token))
                {
                    errors.Add($"Duplicate token reference: {token.TypeName} at line {token.StartLine}");
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Finds the first position where two strings differ.
        /// </summary>
        private static int FindFirstDifference(string a, string b)
        {
            var minLen = Math.Min(a.Length, b.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (a[i] != b[i])
                    return i;
            }
            return minLen;
        }

        /// <summary>
        /// Gets a substring safely, handling bounds and escaping special characters.
        /// </summary>
        private static string SafeSubstring(string s, int start, int len)
        {
            if (start >= s.Length)
                return "<end>";

            var substring = s.Substring(start, Math.Min(len, s.Length - start));
            return substring.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        /// <summary>
        /// Truncates a string to a maximum length.
        /// </summary>
        private static string Truncate(string s, int maxLength)
        {
            if (s == null)
                return null;
            if (s.Length <= maxLength)
                return s;
            return s.Substring(0, maxLength - 3) + "...";
        }

        #endregion

        #region Comparison Methods

        /// <summary>
        /// Compares two AST trees for structural equivalence.
        /// Useful for comparing incremental parse results with fresh parse results.
        /// </summary>
        /// <param name="tree1">First tree.</param>
        /// <param name="tree2">Second tree.</param>
        /// <returns>List of differences found.</returns>
        public static IReadOnlyList<string> CompareStructure(GDClassDeclaration tree1, GDClassDeclaration tree2)
        {
            var differences = new List<string>();

            if (tree1 == null && tree2 == null)
                return differences;

            if (tree1 == null)
            {
                differences.Add("First tree is null");
                return differences;
            }

            if (tree2 == null)
            {
                differences.Add("Second tree is null");
                return differences;
            }

            // Compare text output
            var text1 = tree1.ToString();
            var text2 = tree2.ToString();

            if (text1 != text2)
            {
                var diffPos = FindFirstDifference(text1, text2);
                differences.Add($"Text differs at position {diffPos}. " +
                    $"Tree1[{diffPos}..]: '{SafeSubstring(text1, diffPos, 20)}', " +
                    $"Tree2[{diffPos}..]: '{SafeSubstring(text2, diffPos, 20)}'");
            }

            // Compare node counts
            var nodeCount1 = tree1.AllNodes.Count();
            var nodeCount2 = tree2.AllNodes.Count();
            if (nodeCount1 != nodeCount2)
            {
                differences.Add($"Node count differs: tree1={nodeCount1}, tree2={nodeCount2}");
            }

            // Compare token counts
            var tokenCount1 = tree1.AllTokens.Count();
            var tokenCount2 = tree2.AllTokens.Count();
            if (tokenCount1 != tokenCount2)
            {
                differences.Add($"Token count differs: tree1={tokenCount1}, tree2={tokenCount2}");
            }

            // Recursive structural comparison
            CompareNodesRecursive(tree1, tree2, "", differences);

            return differences;
        }

        /// <summary>
        /// Recursively compares two nodes and their children.
        /// </summary>
        private static void CompareNodesRecursive(GDNode node1, GDNode node2, string path, List<string> differences)
        {
            if (node1.GetType() != node2.GetType())
            {
                differences.Add($"Type mismatch at {path}: {node1.GetType().Name} vs {node2.GetType().Name}");
                return;
            }

            var tokens1 = node1.Tokens.ToList();
            var tokens2 = node2.Tokens.ToList();

            if (tokens1.Count != tokens2.Count)
            {
                differences.Add($"Token count at {path}: {tokens1.Count} vs {tokens2.Count}");
                return;
            }

            for (int i = 0; i < tokens1.Count; i++)
            {
                var token1 = tokens1[i];
                var token2 = tokens2[i];
                var childPath = $"{path}/{node1.GetType().Name}[{i}]";

                if (token1 is GDNode childNode1 && token2 is GDNode childNode2)
                {
                    CompareNodesRecursive(childNode1, childNode2, childPath, differences);
                }
                else
                {
                    if (token1.GetType() != token2.GetType())
                    {
                        differences.Add($"Token type at {childPath}: {token1.GetType().Name} vs {token2.GetType().Name}");
                    }
                    else if (token1.ToString() != token2.ToString())
                    {
                        differences.Add($"Token text at {childPath}: '{Truncate(token1.ToString(), 20)}' vs '{Truncate(token2.ToString(), 20)}'");
                    }
                }
            }
        }

        #endregion

        #region ReferenceEqualityComparer

        /// <summary>
        /// Comparer for object identity (reference equality).
        /// </summary>
        private class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        #endregion
    }
}
