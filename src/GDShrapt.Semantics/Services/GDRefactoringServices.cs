namespace GDShrapt.Semantics;

/// <summary>
/// Unified access to all refactoring and code action services.
/// Provides lazy-initialized services for code transformations, navigation, and analysis.
///
/// <para>
/// Obtain this through <see cref="GDProjectSemanticModel.Services"/>.
/// </para>
///
/// <example>
/// <code>
/// var model = GDProjectSemanticModel.Load("/path/to/project");
///
/// // Find all references to a symbol
/// var refs = model.Services.FindReferences.FindReferences(context);
///
/// // Rename a symbol across the project
/// var plan = model.Services.Rename.PlanRename(context, "newName");
///
/// // Extract a variable
/// var result = model.Services.ExtractVariable.ExtractVariable(context, "varName");
/// </code>
/// </example>
/// </summary>
public class GDRefactoringServices
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    // Lazy-initialized services (project-aware)
    private GDRenameService? _rename;
    private GDFindReferencesService? _findReferences;

    // Lazy-initialized services (stateless)
    private GDGoToDefinitionService? _goToDefinition;
    private GDAddTypeAnnotationsService? _addTypeAnnotations;
    private GDAddTypeAnnotationService? _addTypeAnnotation;
    private GDExtractMethodService? _extractMethod;
    private GDExtractVariableService? _extractVariable;
    private GDExtractConstantService? _extractConstant;
    private GDReorderMembersService? _reorderMembers;
    private GDGenerateGetterSetterService? _generateGetterSetter;
    private GDGenerateOnreadyService? _generateOnready;
    private GDFormatCodeService? _formatCode;
    private GDConvertForToWhileService? _convertForToWhile;
    private GDInvertConditionService? _invertCondition;
    private GDRemoveCommentsService? _removeComments;
    private GDSnippetService? _snippets;
    private GDSurroundWithService? _surroundWith;

    internal GDRefactoringServices(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project ?? throw new System.ArgumentNullException(nameof(project));
        _projectModel = projectModel;
    }

    #region Navigation Services

    /// <summary>
    /// Navigate to symbol definition.
    /// Resolves the declaration location for identifiers, types, and references.
    /// </summary>
    public GDGoToDefinitionService GoToDefinition => _goToDefinition ??= new GDGoToDefinitionService();

    /// <summary>
    /// Find all references to a symbol across the project.
    /// Includes confidence levels for dynamic/duck-typed references.
    /// </summary>
    public GDFindReferencesService FindReferences => _findReferences ??= new GDFindReferencesService(_project, _projectModel);

    #endregion

    #region Rename Services

    /// <summary>
    /// Rename symbol across the project.
    /// Plans and executes symbol renames with conflict detection.
    /// </summary>
    public GDRenameService Rename => _rename ??= new GDRenameService(_project, _projectModel);

    #endregion

    #region Extract Refactorings

    /// <summary>
    /// Extract code block into a new method.
    /// Analyzes dependencies and generates appropriate parameters/return type.
    /// </summary>
    public GDExtractMethodService ExtractMethod => _extractMethod ??= new GDExtractMethodService();

    /// <summary>
    /// Extract expression into a local variable.
    /// Infers type and suggests variable name.
    /// </summary>
    public GDExtractVariableService ExtractVariable => _extractVariable ??= new GDExtractVariableService();

    /// <summary>
    /// Extract literal into a class-level constant.
    /// Moves magic numbers/strings to named constants.
    /// </summary>
    public GDExtractConstantService ExtractConstant => _extractConstant ??= new GDExtractConstantService();

    #endregion

    #region Type Annotation Services

    /// <summary>
    /// Add type annotations to all untyped code in a file.
    /// Uses type inference to suggest annotations for variables, parameters, and returns.
    /// </summary>
    public GDAddTypeAnnotationsService AddTypeAnnotations => _addTypeAnnotations ??= new GDAddTypeAnnotationsService();

    /// <summary>
    /// Add type annotation to a single declaration.
    /// </summary>
    public GDAddTypeAnnotationService AddTypeAnnotation => _addTypeAnnotation ??= new GDAddTypeAnnotationService();

    #endregion

    #region Code Generation Services

    /// <summary>
    /// Generate getter/setter for a variable.
    /// Creates property-style accessors or explicit methods.
    /// </summary>
    public GDGenerateGetterSetterService GenerateGetterSetter => _generateGetterSetter ??= new GDGenerateGetterSetterService();

    /// <summary>
    /// Generate @onready variable from $NodePath expressions.
    /// Converts inline node references to cached @onready variables.
    /// </summary>
    public GDGenerateOnreadyService GenerateOnready => _generateOnready ??= new GDGenerateOnreadyService();

    /// <summary>
    /// Code snippets for common patterns.
    /// </summary>
    public GDSnippetService Snippets => _snippets ??= new GDSnippetService();

    /// <summary>
    /// Surround selected code with control structures.
    /// Wraps code in if/for/while/try blocks.
    /// </summary>
    public GDSurroundWithService SurroundWith => _surroundWith ??= new GDSurroundWithService();

    #endregion

    #region Code Organization Services

    /// <summary>
    /// Reorder class members by category.
    /// Organizes signals, enums, constants, variables, and methods.
    /// </summary>
    public GDReorderMembersService ReorderMembers => _reorderMembers ??= new GDReorderMembersService();

    /// <summary>
    /// Format GDScript code.
    /// Applies consistent formatting according to configured rules.
    /// </summary>
    public GDFormatCodeService FormatCode => _formatCode ??= new GDFormatCodeService();

    /// <summary>
    /// Remove comments from code.
    /// Strips single-line and multi-line comments.
    /// </summary>
    public GDRemoveCommentsService RemoveComments => _removeComments ??= new GDRemoveCommentsService();

    #endregion

    #region Control Flow Refactorings

    /// <summary>
    /// Convert for loop to while loop.
    /// </summary>
    public GDConvertForToWhileService ConvertForToWhile => _convertForToWhile ??= new GDConvertForToWhileService();

    /// <summary>
    /// Invert condition in if/while/for statements.
    /// Applies De Morgan's laws for complex conditions.
    /// </summary>
    public GDInvertConditionService InvertCondition => _invertCondition ??= new GDInvertConditionService();

    #endregion
}
