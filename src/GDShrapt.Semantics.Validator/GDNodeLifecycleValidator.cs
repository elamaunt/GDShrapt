using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates node access lifecycle — detects $Node/get_node() in class-level initializers without @onready.
/// Reports GD7018 (NodeAccessBeforeReady).
/// </summary>
public class GDNodeLifecycleValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDDiagnosticSeverity _severity;

    public GDNodeLifecycleValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions options)
        : base(context)
    {
        _semanticModel = semanticModel;
        _severity = GDDiagnosticSeverity.Warning;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDVariableDeclaration varDecl)
    {
        if (varDecl.Initializer == null)
            return;

        if (varDecl.AttributesDeclaredBefore.Any(attr => attr.Attribute?.IsOnready() == true))
            return;

        if (varDecl.ConstKeyword != null)
            return;

        var detector = new GDNodeAccessDetector();
        varDecl.Initializer.WalkIn(detector);

        if (detector.HasNodeAccess)
        {
            var message = $"Node access in class-level initializer without @onready — node tree is not available yet. Add @onready annotation";
            ReportDiagnostic(GDDiagnosticCode.NodeAccessBeforeReady, message, varDecl);
        }
    }

    private void ReportDiagnostic(GDDiagnosticCode code, string message, GDNode node)
    {
        switch (_severity)
        {
            case GDDiagnosticSeverity.Error:
                ReportError(code, message, node);
                break;
            case GDDiagnosticSeverity.Warning:
                ReportWarning(code, message, node);
                break;
            case GDDiagnosticSeverity.Hint:
                ReportHint(code, message, node);
                break;
        }
    }

    /// <summary>
    /// Inner visitor that detects any node access expression in an initializer.
    /// </summary>
    private sealed class GDNodeAccessDetector : GDVisitor
    {
        public bool HasNodeAccess { get; private set; }

        public override void Visit(GDGetNodeExpression expr) => HasNodeAccess = true;
        public override void Visit(GDGetUniqueNodeExpression expr) => HasNodeAccess = true;

        public override void Visit(GDCallExpression call)
        {
            var name = GDNodePathExtractor.GetCallName(call);
            if (name is "get_node" or "get_node_or_null" or "find_node")
                HasNodeAccess = true;
        }
    }
}
