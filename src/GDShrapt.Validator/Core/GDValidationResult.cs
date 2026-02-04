using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Contains all diagnostics found during validation.
    /// Provides filtering by severity, line, and error code.
    /// </summary>
    public class GDValidationResult
    {
        private readonly List<GDDiagnostic> _diagnostics;

        public IReadOnlyList<GDDiagnostic> Diagnostics => _diagnostics;

        public bool HasErrors => _diagnostics.Any(d => d.Severity == GDDiagnosticSeverity.Error);
        public bool HasWarnings => _diagnostics.Any(d => d.Severity == GDDiagnosticSeverity.Warning);
        public bool HasHints => _diagnostics.Any(d => d.Severity == GDDiagnosticSeverity.Hint);

        /// <summary>
        /// True if no errors (warnings/hints are allowed).
        /// </summary>
        public bool IsValid => !HasErrors;

        public IEnumerable<GDDiagnostic> Errors => _diagnostics.Where(d => d.Severity == GDDiagnosticSeverity.Error);
        public IEnumerable<GDDiagnostic> Warnings => _diagnostics.Where(d => d.Severity == GDDiagnosticSeverity.Warning);
        public IEnumerable<GDDiagnostic> Hints => _diagnostics.Where(d => d.Severity == GDDiagnosticSeverity.Hint);

        public GDValidationResult()
        {
            _diagnostics = new List<GDDiagnostic>();
        }

        public GDValidationResult(IEnumerable<GDDiagnostic> diagnostics)
        {
            _diagnostics = diagnostics?.ToList() ?? new List<GDDiagnostic>();
        }

        internal void Add(GDDiagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        internal void AddRange(IEnumerable<GDDiagnostic> diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }

        internal void Merge(GDValidationResult other)
        {
            if (other != null)
            {
                _diagnostics.AddRange(other._diagnostics);
            }
        }

        /// <summary>
        /// Gets all diagnostics that span the given line.
        /// </summary>
        public IEnumerable<GDDiagnostic> GetDiagnosticsAtLine(int line)
        {
            return _diagnostics.Where(d => d.StartLine <= line && d.EndLine >= line);
        }

        /// <summary>
        /// Filters diagnostics by error code (e.g. BreakOutsideLoop).
        /// </summary>
        public IEnumerable<GDDiagnostic> GetDiagnosticsByCode(GDDiagnosticCode code)
        {
            return _diagnostics.Where(d => d.Code == code);
        }

        /// <summary>
        /// Removes diagnostics that are suppressed by comment directives.
        /// </summary>
        /// <param name="suppressionContext">The suppression context containing parsed directives.</param>
        public void FilterSuppressed(global::GDShrapt.Validator.GDValidatorSuppressionContext suppressionContext)
        {
            if (suppressionContext == null)
                return;

            _diagnostics.RemoveAll(d =>
                suppressionContext.IsSuppressed(d.Code.ToCodeString(), d.StartLine));
        }
    }
}
