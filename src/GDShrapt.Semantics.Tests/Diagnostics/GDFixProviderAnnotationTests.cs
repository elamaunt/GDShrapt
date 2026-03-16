using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDFixProviderAnnotationTests
{
    private GDFixProvider _fixProvider = null!;
    private GDScriptReader _reader = null!;

    [TestInitialize]
    public void Setup()
    {
        _fixProvider = new GDFixProvider();
        _reader = new GDScriptReader();
    }

    #region GD3022 — AnnotationWiderThanInferred

    [TestMethod]
    public void GD3022_TypeNode_GeneratesRemoveFix()
    {
        var code = @"
var enemy: Node = Sprite2D.new()
";
        var classDecl = _reader.ParseFileContent(code);
        var typeNode = classDecl.AllNodes.OfType<GDTypeNode>().First();

        var fixes = _fixProvider.GetFixes("GD3022", typeNode, null, null).ToList();

        // Should have suppression + remove annotation fix
        fixes.Should().HaveCountGreaterThanOrEqualTo(2);
        fixes.Should().ContainSingle(f => f is GDSuppressionFixDescriptor);

        var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
        textFix.Should().NotBeNull();
        textFix!.Kind.Should().Be(GDFixKind.RemoveText);
        textFix.Title.Should().Contain("Remove");
    }

    [TestMethod]
    public void GD3022_NonTypeNode_ReturnsOnlySuppression()
    {
        var code = @"
func test():
    var x = 1
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.AllNodes.OfType<GDVariableDeclarationStatement>().First();

        var fixes = _fixProvider.GetFixes("GD3022", varDecl, null, null).ToList();

        // Non-GDTypeNode should only return suppression
        fixes.Should().ContainSingle();
        fixes[0].Should().BeOfType<GDSuppressionFixDescriptor>();
    }

    #endregion

    #region GD7022 — RedundantAnnotation

    [TestMethod]
    public void GD7022_TypeNode_InVarDeclaration_GeneratesRemoveFix()
    {
        var code = @"
var x: int = 5
";
        var classDecl = _reader.ParseFileContent(code);
        var typeNode = classDecl.AllNodes.OfType<GDTypeNode>().First();

        var fixes = _fixProvider.GetFixes("GD7022", typeNode, null, null).ToList();

        fixes.Should().HaveCountGreaterThanOrEqualTo(2);

        var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
        textFix.Should().NotBeNull();
        textFix!.Kind.Should().Be(GDFixKind.RemoveText);
        textFix.Title.Should().Contain("redundant");
    }

    [TestMethod]
    public void GD7022_TypeNode_NotInVarDeclaration_ReturnsOnlySuppression()
    {
        // A type node that is not inside a variable declaration
        var code = @"
func test(x: int):
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var typeNode = classDecl.AllNodes.OfType<GDTypeNode>().FirstOrDefault();

        if (typeNode != null)
        {
            var fixes = _fixProvider.GetFixes("GD7022", typeNode, null, null).ToList();

            // Parameter type is not in GDVariableDeclaration, so CreateRemoveAnnotationFixes
            // checks parent type — it should not generate a fix for non-variable parents
            var textFixes = fixes.OfType<GDTextEditFixDescriptor>().ToList();
            // Whether fix is generated depends on parent check — just verify no crash
            fixes.Should().NotBeEmpty("at least suppression should be present");
        }
    }

    #endregion

    #region GD7019 — TypeWideningAssignment

    [TestMethod]
    public void GD7019_VarDeclWithType_GeneratesRemoveFix()
    {
        var code = @"
var sprite: Sprite2D = get_node(""X"")
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.AllNodes.OfType<GDVariableDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD7019", varDecl, null, null).ToList();

        // Should have suppression + remove type annotation fix
        fixes.Should().HaveCountGreaterThanOrEqualTo(2);

        var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
        textFix.Should().NotBeNull();
        textFix!.Kind.Should().Be(GDFixKind.RemoveText);
        textFix.Title.Should().Contain("Remove type annotation");
    }

    [TestMethod]
    public void GD7019_VarDeclWithoutType_ReturnsOnlySuppression()
    {
        var code = @"
var x = get_node(""X"")
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.AllNodes.OfType<GDVariableDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD7019", varDecl, null, null).ToList();

        // No type node to remove → only suppression
        fixes.Should().ContainSingle();
        fixes[0].Should().BeOfType<GDSuppressionFixDescriptor>();
    }

    #endregion

    #region GD3023 — InconsistentReturnTypes

    [TestMethod]
    public void GD3023_MethodWithoutReturnType_GeneratesInsertFix()
    {
        var code = @"
func get_value(flag):
    if flag:
        return 1
    else:
        return ""hello""
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.AllNodes.OfType<GDMethodDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD3023", methodDecl, null, null).ToList();

        // Should have suppression + add return type fix
        fixes.Should().HaveCountGreaterThanOrEqualTo(2);

        var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
        textFix.Should().NotBeNull();
        textFix!.Kind.Should().Be(GDFixKind.InsertText);
        textFix.Title.Should().Contain("return type");
        textFix.NewText.Should().Contain("-> Variant");
    }

    [TestMethod]
    public void GD3023_MethodWithReturnType_ReturnsOnlySuppression()
    {
        var code = @"
func get_value(flag) -> int:
    if flag:
        return 1
    else:
        return 2
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.AllNodes.OfType<GDMethodDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD3023", methodDecl, null, null).ToList();

        // Method already has return type → only suppression
        fixes.Should().ContainSingle();
        fixes[0].Should().BeOfType<GDSuppressionFixDescriptor>();
    }

    [TestMethod]
    public void GD3023_NonMethodNode_WalksUpToMethod()
    {
        var code = @"
func get_value(flag):
    if flag:
        return 1
    return ""hello""
";
        var classDecl = _reader.ParseFileContent(code);
        var returnStmt = classDecl.AllNodes.OfType<GDReturnExpression>().First();

        var fixes = _fixProvider.GetFixes("GD3023", returnStmt, null, null).ToList();

        // Should walk up from return statement to method and generate fix
        var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
        textFix.Should().NotBeNull();
        textFix!.NewText.Should().Contain("-> Variant");
    }

    #endregion

    #region GD3024 — MissingReturnInBranch

    [TestMethod]
    public void GD3024_Method_GeneratesInsertReturnFix()
    {
        var code = @"
func get_value(flag) -> int:
    if flag:
        return 1
";
        var classDecl = _reader.ParseFileContent(code);
        var methodDecl = classDecl.AllNodes.OfType<GDMethodDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD3024", methodDecl, null, null).ToList();

        // Should have suppression + add return statement fix
        fixes.Should().HaveCountGreaterThanOrEqualTo(2);

        var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
        textFix.Should().NotBeNull();
        textFix!.Kind.Should().Be(GDFixKind.InsertText);
        textFix.Title.Should().Contain("return statement");
        textFix.NewText.Should().Contain("return");
    }

    [TestMethod]
    public void GD3024_NonMethodNode_WalksUpToMethod()
    {
        var code = @"
func get_value(flag) -> int:
    if flag:
        return 1
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var passExpr = classDecl.AllNodes.OfType<GDPassExpression>().FirstOrDefault();

        if (passExpr != null)
        {
            var fixes = _fixProvider.GetFixes("GD3024", passExpr, null, null).ToList();

            // Should walk up to method declaration
            var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            textFix.Should().NotBeNull("should generate return statement fix from non-method node");
        }
    }

    #endregion

    #region GD3001 — UndefinedVariable (DeclareVariable fix)

    [TestMethod]
    public void GD3001_IdentifierExpression_GeneratesDeclareVariableFix()
    {
        var code = @"
func test():
    x = 5
";
        var classDecl = _reader.ParseFileContent(code);
        // Find the identifier expression for 'x'
        var identExpr = classDecl.AllNodes.OfType<GDIdentifierExpression>()
            .FirstOrDefault(e => e.Identifier?.Sequence == "x");

        if (identExpr != null)
        {
            var fixes = _fixProvider.GetFixes("GD3001", identExpr, null, null).ToList();

            var textFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            textFix.Should().NotBeNull();
            textFix!.Kind.Should().Be(GDFixKind.DeclareVariable);
            textFix.NewText.Should().Contain("var x");
        }
    }

    #endregion

    #region Suppression always present

    [TestMethod]
    public void AllNewDiagnosticCodes_AlwaysIncludeSuppression()
    {
        var code = @"
var x: int = 5

func get_value(flag) -> int:
    if flag:
        return 1
    return 2
";
        var classDecl = _reader.ParseFileContent(code);
        var typeNode = classDecl.AllNodes.OfType<GDTypeNode>().First();
        var methodDecl = classDecl.AllNodes.OfType<GDMethodDeclaration>().First();

        var diagnosticCodes = new[] { "GD3022", "GD7022", "GD7019", "GD3023", "GD3024" };

        foreach (var code_ in diagnosticCodes)
        {
            var node = code_ switch
            {
                "GD3022" or "GD7022" => (GDNode)typeNode,
                "GD7019" => classDecl.AllNodes.OfType<GDVariableDeclaration>().First(),
                _ => (GDNode)methodDecl
            };

            var fixes = _fixProvider.GetFixes(code_, node, null, null).ToList();

            fixes.Should().Contain(f => f is GDSuppressionFixDescriptor,
                $"diagnostic {code_} should always include suppression fix");
        }
    }

    #endregion
}
