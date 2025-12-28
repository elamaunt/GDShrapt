namespace GDShrapt.Reader
{
    /// <summary>
    /// Reports all GDInvalidToken nodes found during parsing.
    /// </summary>
    public class GDSyntaxValidator : GDValidationVisitor
    {
        public GDSyntaxValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            foreach (var invalidToken in node.AllInvalidTokens)
            {
                Context.AddError(
                    GDDiagnosticCode.InvalidToken,
                    $"Invalid token: '{invalidToken.Sequence}'",
                    node);
            }
        }
    }
}
