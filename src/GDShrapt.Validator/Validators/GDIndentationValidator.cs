using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates indentation consistency in GDScript code.
    /// Checks for:
    /// - Mixing tabs and spaces
    /// - Unexpected indentation changes
    /// - Consistent indentation levels
    /// </summary>
    public class GDIndentationValidator : GDValidationVisitor
    {
        private enum IndentStyle
        {
            Unknown,
            Tabs,
            Spaces
        }

        private IndentStyle _detectedStyle = IndentStyle.Unknown;
        private readonly Stack<int> _indentLevels = new Stack<int>();
        private int _expectedSpacesPerIndent = 0;

        public GDIndentationValidator(GDValidationContext context) : base(context)
        {
            _indentLevels.Push(0);
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            // AllTokens already iterates in source order, no need to sort or materialize
            foreach (var intendation in node.AllTokens.OfType<GDIntendation>())
            {
                ValidateIntendation(intendation);
            }
        }

        private void ValidateIntendation(GDIntendation intendation)
        {
            var sequence = intendation.Sequence ?? string.Empty;

            // Skip empty indentation (line at column 0)
            if (string.IsNullOrEmpty(sequence))
            {
                return;
            }

            // Check for mixed tabs and spaces
            bool hasTabs = sequence.Contains('\t');
            bool hasSpaces = sequence.Contains(' ');

            if (hasTabs && hasSpaces)
            {
                Context.AddWarning(
                    GDDiagnosticCode.InconsistentIndentation,
                    "Mixed tabs and spaces in indentation",
                    intendation);
                return;
            }

            // Detect indentation style
            if (_detectedStyle == IndentStyle.Unknown)
            {
                _detectedStyle = hasTabs ? IndentStyle.Tabs : IndentStyle.Spaces;
                if (_detectedStyle == IndentStyle.Spaces)
                {
                    _expectedSpacesPerIndent = sequence.Length;
                }
            }
            else
            {
                // Check consistency with detected style
                if (_detectedStyle == IndentStyle.Tabs && hasSpaces)
                {
                    Context.AddWarning(
                        GDDiagnosticCode.InconsistentIndentation,
                        "Expected tabs but found spaces",
                        intendation);
                }
                else if (_detectedStyle == IndentStyle.Spaces && hasTabs)
                {
                    Context.AddWarning(
                        GDDiagnosticCode.InconsistentIndentation,
                        "Expected spaces but found tabs",
                        intendation);
                }
            }

            // Calculate indentation level
            int currentLevel = CalculateIndentLevel(sequence);
            int previousLevel = _indentLevels.Peek();

            // Skip level-jump validation for tokens inside expression contexts
            // (lambda bodies, multi-line function arguments, array/dictionary initializers).
            // Indentation inside parentheses is continuation indentation, not statement-level.
            if (IsInsideExpressionContext(intendation))
                return;

            // Check for valid indentation changes
            if (currentLevel > previousLevel)
            {
                // Indent should only increase by one level
                if (currentLevel > previousLevel + 1)
                {
                    Context.AddError(
                        GDDiagnosticCode.UnexpectedIndent,
                        $"Unexpected indentation: jumped from level {previousLevel} to {currentLevel}",
                        intendation);
                }
                _indentLevels.Push(currentLevel);
            }
            else if (currentLevel < previousLevel)
            {
                // Dedent - pop levels until we match
                while (_indentLevels.Count > 1 && _indentLevels.Peek() > currentLevel)
                {
                    _indentLevels.Pop();
                }

                // Check if we landed on a valid level
                if (_indentLevels.Peek() != currentLevel && currentLevel != 0)
                {
                    Context.AddError(
                        GDDiagnosticCode.IndentationMismatch,
                        $"Indentation does not match any outer level (found level {currentLevel})",
                        intendation);
                }
            }
        }

        private static bool IsInsideExpressionContext(GDSyntaxToken token)
        {
            var node = token.Parent;
            while (node != null)
            {
                if (node is GDExpressionsList ||
                    node is GDCallExpression ||
                    node is GDMethodExpression ||
                    node is GDArrayInitializerExpression ||
                    node is GDDictionaryInitializerExpression ||
                    node is GDBracketExpression)
                {
                    return true;
                }

                node = node.Parent;
            }
            return false;
        }

        private int CalculateIndentLevel(string sequence)
        {
            if (_detectedStyle == IndentStyle.Tabs)
            {
                return sequence.Count(c => c == '\t');
            }
            else if (_detectedStyle == IndentStyle.Spaces && _expectedSpacesPerIndent > 0)
            {
                return sequence.Length / _expectedSpacesPerIndent;
            }
            return 0;
        }
    }
}
