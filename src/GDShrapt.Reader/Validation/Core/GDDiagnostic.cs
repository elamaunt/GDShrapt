namespace GDShrapt.Reader
{
    /// <summary>
    /// A single validation issue with severity, code, message and location.
    /// </summary>
    public class GDDiagnostic
    {
        public GDDiagnosticSeverity Severity { get; }
        public GDDiagnosticCode Code { get; }
        public string Message { get; }

        /// <summary>Line numbers are 1-based.</summary>
        public int StartLine { get; }
        /// <summary>Column numbers are 0-based.</summary>
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }

        /// <summary>The AST node that caused this diagnostic (may be null).</summary>
        public GDNode Node { get; }

        public GDDiagnostic(
            GDDiagnosticSeverity severity,
            GDDiagnosticCode code,
            string message,
            GDNode node)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Node = node;

            if (node != null)
            {
                StartLine = node.StartLine;
                StartColumn = node.StartColumn;
                EndLine = node.EndLine;
                EndColumn = node.EndColumn;
            }
        }

        /// <summary>
        /// Creates a diagnostic with explicit location (without AST node).
        /// </summary>
        public GDDiagnostic(
            GDDiagnosticSeverity severity,
            GDDiagnosticCode code,
            string message,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            Severity = severity;
            Code = code;
            Message = message;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        /// <summary>
        /// Creates a diagnostic from a syntax token (not a node).
        /// </summary>
        public GDDiagnostic(
            GDDiagnosticSeverity severity,
            GDDiagnosticCode code,
            string message,
            GDSyntaxToken token)
        {
            Severity = severity;
            Code = code;
            Message = message;

            if (token != null)
            {
                StartLine = token.StartLine;
                StartColumn = token.StartColumn;
                EndLine = token.EndLine;
                EndColumn = token.EndColumn;
            }
        }

        public static GDDiagnostic Error(GDDiagnosticCode code, string message, GDNode node)
            => new GDDiagnostic(GDDiagnosticSeverity.Error, code, message, node);

        public static GDDiagnostic Error(GDDiagnosticCode code, string message, GDSyntaxToken token)
            => new GDDiagnostic(GDDiagnosticSeverity.Error, code, message, token);

        public static GDDiagnostic Warning(GDDiagnosticCode code, string message, GDNode node)
            => new GDDiagnostic(GDDiagnosticSeverity.Warning, code, message, node);

        public static GDDiagnostic Warning(GDDiagnosticCode code, string message, GDSyntaxToken token)
            => new GDDiagnostic(GDDiagnosticSeverity.Warning, code, message, token);

        public static GDDiagnostic Hint(GDDiagnosticCode code, string message, GDNode node)
            => new GDDiagnostic(GDDiagnosticSeverity.Hint, code, message, node);

        public static GDDiagnostic Hint(GDDiagnosticCode code, string message, GDSyntaxToken token)
            => new GDDiagnostic(GDDiagnosticSeverity.Hint, code, message, token);

        /// <summary>
        /// Formatted code like "GD5001".
        /// </summary>
        public string CodeString => Code.ToCodeString();

        /// <summary>
        /// Compiler-style: "error GD5001: message (3:5)"
        /// </summary>
        public override string ToString()
        {
            var severity = Severity.ToString().ToLowerInvariant();
            return $"{severity} {CodeString}: {Message} ({StartLine}:{StartColumn})";
        }

        /// <summary>
        /// With full range: "error GD5001 at 3:5-3:10: message"
        /// </summary>
        public string ToDetailedString()
        {
            var severity = Severity.ToString().ToLowerInvariant();
            return $"{severity} {CodeString} at {StartLine}:{StartColumn}-{EndLine}:{EndColumn}: {Message}";
        }
    }
}
