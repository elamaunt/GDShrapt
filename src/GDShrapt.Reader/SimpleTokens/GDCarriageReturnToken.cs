using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a carriage return (\r) character in the source code.
    /// This token is created automatically during parsing and cannot be created manually.
    /// It does not affect Godot position calculations but is preserved for exact text reconstruction.
    /// </summary>
    public sealed class GDCarriageReturnToken : GDSingleCharToken
    {
        /// <summary>
        /// The carriage return character.
        /// </summary>
        public override char Char => '\r';

        /// <summary>
        /// Length is 0 for Godot position compatibility.
        /// Carriage returns are invisible to Godot's position system.
        /// </summary>
        public override int Length => 0;

        /// <summary>
        /// No line breaks - carriage return alone does not create a new line.
        /// </summary>
        public override int NewLinesCount => 0;

        /// <summary>
        /// Origin length includes the \r character for text-based operations.
        /// </summary>
        public override int OriginLength => 1;

        /// <summary>
        /// Column does not change after a carriage return (for Godot positions).
        /// </summary>
        public override int EndColumn => StartColumn;

        /// <summary>
        /// Creates a clone of this token.
        /// </summary>
        public override GDSyntaxToken Clone() => new GDCarriageReturnToken();

        /// <summary>
        /// Returns an empty string for Godot-compatible output.
        /// Use ToOriginalString() on parent nodes to include \r characters.
        /// </summary>
        public override string ToString() => string.Empty;

        /// <summary>
        /// Appends this token to the builder.
        /// </summary>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="includeIgnored">If true, includes the \r character; otherwise omits it.</param>
        public override void AppendTo(StringBuilder builder, bool includeIgnored)
        {
            if (includeIgnored)
                builder.Append('\r');
            // When includeIgnored is false, we don't append anything (Godot-compatible mode)
        }

        /// <summary>
        /// Creates a new carriage return token.
        /// </summary>
        public GDCarriageReturnToken() { }
    }
}
