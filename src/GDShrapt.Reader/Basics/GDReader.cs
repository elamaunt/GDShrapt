namespace GDShrapt.Reader
{
    public abstract class GDReader 
    {
        /// <summary>
        /// Pass single character in the node. 
        /// If the node can't handle the character it may return the character to previous node in reading state.
        /// </summary>
        /// <param name="c">Character</param>
        /// <param name="state">Current reading state</param>
        internal abstract void HandleChar(char c, GDReadingState state);

        /// <summary>
        /// The same <see cref="HandleChar(char, GDReadingState)"/> but separated method for new line character
        /// </summary>
        /// <param name="state">Current reading state</param>
        internal abstract void HandleNewLineChar(GDReadingState state);

        /// <summary>
        /// Simple check on whitespace characters ' ' and '\t'.
        /// </summary>
        /// <param name="c">One char to check</param>
        internal bool IsSpace(char c) => c == ' ' || c == '\t';

        internal bool IsIdentifierStartChar(char c) => c == '_' || char.IsLetter(c);
        internal bool IsStringStartChar(char c) => c == '\'' || c == '\"';
        internal bool IsExpressionStopChar(char c) => c == ',' || c == '}' || c == ')' || c == ']' || c == ':' || c == ';';
        internal bool IsNumberStartChar(char c) => char.IsDigit(c);


        /// <summary>
        /// The same <see cref="HandleChar(char, GDReadingState)"/> but separated method for sharp (line commentary) character.
        /// Default implementation will add a new comment token in the reading state.
        /// </summary>
        /// <param name="state">Current reading state</param>
        internal abstract void HandleSharpChar(GDReadingState state);



        /// <summary>
        /// Force completes token characters handling process in terms of current reading state.
        /// Used for situation when the reading code has ended.
        /// </summary>
        /// <param name="state">Current reading state</param>
        internal virtual void ForceComplete(GDReadingState state)
        {
            state.Pop();
        }
    }
}