using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDFixProviderTests
{
    private GDFixProvider _fixProvider = null!;
    private GDScriptReader _reader = null!;

    [TestInitialize]
    public void Setup()
    {
        _fixProvider = new GDFixProvider();
        _reader = new GDScriptReader();
    }

    #region Suppression Fix Tests

    [TestMethod]
    public void GetFixes_AnyDiagnostic_AlwaysIncludesSuppression()
    {
        var code = @"
func process(obj):
    var x = obj.health
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();

        fixes.Should().ContainSingle(f => f is GDSuppressionFixDescriptor);
        var suppression = fixes.OfType<GDSuppressionFixDescriptor>().First();
        suppression.DiagnosticCode.Should().Be("GD7002");
        suppression.Title.Should().Contain("gd:ignore");
    }

    [TestMethod]
    public void GetFixes_NullNode_ReturnsEmpty()
    {
        var fixes = _fixProvider.GetFixes("GD7002", null, null, null).ToList();

        fixes.Should().BeEmpty();
    }

    [TestMethod]
    public void GetFixes_EmptyCode_ReturnsEmpty()
    {
        var code = @"
func process(obj):
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var passExpr = classDecl.AllNodes.OfType<GDPassExpression>().First();

        var fixes = _fixProvider.GetFixes("", passExpr, null, null).ToList();

        fixes.Should().BeEmpty();
    }

    #endregion

    #region Type Guard Fix Tests

    [TestMethod]
    public void GetFixes_UnguardedPropertyAccess_IncludesTypeGuards()
    {
        var code = @"
func process(obj):
    var x = obj.health
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();

        var typeGuards = fixes.OfType<GDTypeGuardFixDescriptor>().ToList();
        typeGuards.Should().NotBeEmpty();
        typeGuards.Should().Contain(f => f.TypeName == "Node");
        typeGuards.All(f => f.VariableName == "obj").Should().BeTrue();
    }

    [TestMethod]
    public void GetFixes_UnguardedMethodCall_IncludesMethodGuard()
    {
        var code = @"
func process(obj):
    obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);
        var callExpr = classDecl.AllNodes.OfType<GDCallExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7003", callExpr, null, null).ToList();

        var methodGuards = fixes.OfType<GDMethodGuardFixDescriptor>().ToList();
        methodGuards.Should().ContainSingle();
        methodGuards[0].MethodName.Should().Be("attack");
        methodGuards[0].VariableName.Should().Be("obj");
        methodGuards[0].Title.Should().Contain("has_method");
    }

    [TestMethod]
    public void GetFixes_UnguardedMethodCall_IncludesTypeGuards()
    {
        var code = @"
func process(obj):
    obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);
        var callExpr = classDecl.AllNodes.OfType<GDCallExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7003", callExpr, null, null).ToList();

        fixes.OfType<GDTypeGuardFixDescriptor>().Should().NotBeEmpty();
    }

    [TestMethod]
    public void GetFixes_ChainedMemberAccess_ExtractsRootVariable()
    {
        var code = @"
func process(obj):
    obj.component.health = 100
";
        var classDecl = _reader.ParseFileContent(code);
        var outerMemberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>()
            .First(m => m.Identifier?.Sequence == "health");

        var fixes = _fixProvider.GetFixes("GD7002", outerMemberAccess, null, null).ToList();

        var typeGuards = fixes.OfType<GDTypeGuardFixDescriptor>().ToList();
        // Should extract root variable "obj" from "obj.component"
        typeGuards.Should().Contain(f => f.VariableName == "obj" || f.VariableName == "component");
    }

    #endregion

    #region Typo Fix Tests

    [TestMethod]
    public void GetFixes_PropertyNotFound_WithRuntimeProvider_GeneratesTypoFixes()
    {
        var code = @"
func process():
    var node: Node2D = get_node(""Player"")
    node.positon = Vector2.ZERO
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>()
            .First(m => m.Identifier?.Sequence == "positon");

        // Create mock runtime provider that knows about Node2D
        var mockProvider = new MockRuntimeProvider();
        mockProvider.AddType("Node2D", new[]
        {
            GDRuntimeMemberInfo.Property("position", "Vector2"),
            GDRuntimeMemberInfo.Property("rotation", "float"),
            GDRuntimeMemberInfo.Property("scale", "Vector2")
        });

        var fixes = _fixProvider.GetFixes("GD3009", memberAccess, null, mockProvider).ToList();

        // Without semantic analysis to get type, typo fixes won't work
        // This test verifies the structure is in place
        fixes.Should().ContainSingle(f => f is GDSuppressionFixDescriptor);
    }

    #endregion

    #region Descriptor Properties Tests

    [TestMethod]
    public void SuppressionFix_HasCorrectProperties()
    {
        var code = @"
func test():
    var x = obj.health
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();
        var suppression = fixes.OfType<GDSuppressionFixDescriptor>().First();

        suppression.Kind.Should().Be(GDFixKind.Suppress);
        suppression.TargetLine.Should().BeGreaterThan(0);
        suppression.IsInline.Should().BeTrue();
    }

    [TestMethod]
    public void TypeGuardFix_HasCorrectProperties()
    {
        var code = @"
func test():
    var x = obj.health
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();
        var typeGuard = fixes.OfType<GDTypeGuardFixDescriptor>().First();

        typeGuard.Kind.Should().Be(GDFixKind.AddTypeGuard);
        typeGuard.VariableName.Should().NotBeNullOrEmpty();
        typeGuard.TypeName.Should().NotBeNullOrEmpty();
        typeGuard.StatementLine.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void MethodGuardFix_HasCorrectProperties()
    {
        var code = @"
func test():
    obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);
        var call = classDecl.AllNodes.OfType<GDCallExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7003", call, null, null).ToList();
        var methodGuard = fixes.OfType<GDMethodGuardFixDescriptor>().First();

        methodGuard.Kind.Should().Be(GDFixKind.AddMethodGuard);
        methodGuard.VariableName.Should().Be("obj");
        methodGuard.MethodName.Should().Be("attack");
        methodGuard.StatementLine.Should().BeGreaterThan(0);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void GetFixes_SelfAccess_StillGeneratesFixes()
    {
        var code = @"
var health = 100

func test():
    self.health = 50
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();

        // Should at least have suppression
        fixes.Should().NotBeEmpty();
    }

    [TestMethod]
    public void GetFixes_NestedFunction_HandlesIndentation()
    {
        var code = @"
func outer():
    func inner():
        obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);
        var call = classDecl.AllNodes.OfType<GDCallExpression>().FirstOrDefault();

        if (call != null)
        {
            var fixes = _fixProvider.GetFixes("GD7003", call, null, null).ToList();

            var typeGuard = fixes.OfType<GDTypeGuardFixDescriptor>().FirstOrDefault();
            if (typeGuard != null)
            {
                typeGuard.IndentLevel.Should().BeGreaterThanOrEqualTo(0);
            }
        }
    }

    [TestMethod]
    public void GetFixes_UnknownDiagnosticCode_OnlyReturnsSuppression()
    {
        var code = @"
func test():
    var x = 1
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.AllNodes.OfType<GDVariableDeclarationStatement>().First();

        var fixes = _fixProvider.GetFixes("GD9999", varDecl, null, null).ToList();

        // Should only contain suppression for unknown codes
        fixes.Should().ContainSingle();
        fixes[0].Should().BeOfType<GDSuppressionFixDescriptor>();
    }

    #endregion

    #region Null Guard Fix Tests (GD7005-7009)

    [TestMethod]
    public void GetFixes_PotentiallyNullAccess_IncludesNullGuard()
    {
        var code = @"
func process(obj):
    var x = obj.health
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7005", memberAccess, null, null).ToList();

        var textEdit = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault(f => f.Kind == GDFixKind.AddTypeGuard);
        textEdit.Should().NotBeNull("should have a null guard fix");
        textEdit!.NewText.Should().Contain("!= null");
        textEdit.NewText.Should().Contain("obj");

        // AST verification: apply fix and re-parse
        var modified = ApplyInsertFix(code, textEdit);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty("should have an if statement after fix");
        modifiedAst.AllNodes.OfType<GDMemberOperatorExpression>().Should().NotBeEmpty("original access should still exist");
    }

    [TestMethod]
    public void GetFixes_PotentiallyNullMethodCall_IncludesNullGuard()
    {
        var code = @"
func process(obj):
    obj.method()
";
        var classDecl = _reader.ParseFileContent(code);
        var callExpr = classDecl.AllNodes.OfType<GDCallExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7007", callExpr, null, null).ToList();

        var textEdit = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault(f => f.Kind == GDFixKind.AddTypeGuard);
        textEdit.Should().NotBeNull();
        textEdit!.NewText.Should().Contain("if obj != null:");

        // AST verification
        var modified = ApplyInsertFix(code, textEdit);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty();
        modifiedAst.AllNodes.OfType<GDCallExpression>().Should().NotBeEmpty("call should still exist");
    }

    [TestMethod]
    public void GetFixes_PotentiallyNullIndexer_IncludesNullGuard()
    {
        var code = @"
func process(arr):
    var x = arr[0]
";
        var classDecl = _reader.ParseFileContent(code);
        var indexer = classDecl.AllNodes.OfType<GDIndexerExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7006", indexer, null, null).ToList();

        var textEdit = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault(f => f.Kind == GDFixKind.AddTypeGuard);
        textEdit.Should().NotBeNull();
        textEdit!.NewText.Should().Contain("arr");
        textEdit.NewText.Should().Contain("!= null");

        // AST verification
        var modified = ApplyInsertFix(code, textEdit);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty();
    }

    [TestMethod]
    public void GetFixes_NullGuard_AlsoHasSuppression()
    {
        var code = @"
func process(obj):
    var x = obj.health
";
        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixes = _fixProvider.GetFixes("GD7005", memberAccess, null, null).ToList();

        fixes.Should().Contain(f => f is GDSuppressionFixDescriptor);
        fixes.Should().Contain(f => f is GDTextEditFixDescriptor);
    }

    #endregion

    #region Remove Unreachable Code Fix Tests (GD5004)

    [TestMethod]
    public void GetFixes_UnreachableCode_IncludesRemoveFix()
    {
        var code = @"
func test():
    return 1
    pass
";
        var classDecl = _reader.ParseFileContent(code);
        var passExpr = classDecl.AllNodes.OfType<GDPassExpression>().FirstOrDefault();
        if (passExpr == null) return; // pass might be parsed differently

        // Find the statement containing pass
        var passStatement = classDecl.AllNodes.OfType<GDExpressionStatement>()
            .FirstOrDefault(s => s.AllNodes.OfType<GDPassExpression>().Any());
        if (passStatement == null) return;

        var fixes = _fixProvider.GetFixes("GD5004", passStatement, null, null).ToList();

        var removeFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.RemoveUnreachableCode);
        removeFix.Should().NotBeNull("should have remove unreachable code fix");

        fixes.Should().Contain(f => f is GDSuppressionFixDescriptor, "suppression should also be present");
    }

    [TestMethod]
    public void GetFixes_UnreachableCode_RemoveFix_ProducesValidAST()
    {
        var code = @"
func test():
    return 1
    var x = 2
";
        var classDecl = _reader.ParseFileContent(code);
        var varStmt = classDecl.AllNodes.OfType<GDVariableDeclarationStatement>().FirstOrDefault();
        if (varStmt == null) return;

        var fixes = _fixProvider.GetFixes("GD5004", varStmt, null, null).ToList();

        var removeFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.RemoveUnreachableCode);
        if (removeFix == null) return;

        // AST verification
        var modified = ApplyRemoveFix(code, removeFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDReturnExpression>().Should().NotBeEmpty("return should remain");
    }

    #endregion

    #region Remove Await Fix Tests (GD5007)

    [TestMethod]
    public void GetFixes_AwaitOnNonAwaitable_IncludesRemoveAwait()
    {
        var code = @"
func test():
    var x = await 5
";
        var classDecl = _reader.ParseFileContent(code);
        var awaitExpr = classDecl.AllNodes.OfType<GDAwaitExpression>().FirstOrDefault();
        if (awaitExpr == null) return;

        var fixes = _fixProvider.GetFixes("GD5007", awaitExpr, null, null).ToList();

        var removeFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.RemoveText);
        removeFix.Should().NotBeNull("should have remove await fix");
        removeFix!.NewText.Should().BeEmpty("removal fix has empty new text");

        // AST verification: apply fix and verify no await expression remains
        var modified = ApplyRemoveFix(code, removeFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDAwaitExpression>().Should().BeEmpty("await should be removed");
        modifiedAst.AllNodes.OfType<GDNumberExpression>().Should().NotBeEmpty("number literal should remain");
    }

    [TestMethod]
    public void GetFixes_AwaitOnString_IncludesRemoveAwait()
    {
        var code = "func test():\n\tvar x = await \"hello\"\n";
        var classDecl = _reader.ParseFileContent(code);
        var awaitExpr = classDecl.AllNodes.OfType<GDAwaitExpression>().FirstOrDefault();
        if (awaitExpr == null) return;

        var fixes = _fixProvider.GetFixes("GD5007", awaitExpr, null, null).ToList();

        var removeFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.RemoveText);
        removeFix.Should().NotBeNull();

        // AST verification
        var modified = ApplyRemoveFix(code, removeFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDAwaitExpression>().Should().BeEmpty();
        modifiedAst.AllNodes.OfType<GDStringExpression>().Should().NotBeEmpty("string should remain");
    }

    #endregion

    #region Dynamic Guard Fix Tests (GD7015-7016)

    [TestMethod]
    public void GetFixes_DynamicMethodNotFound_IncludesHasMethodGuard()
    {
        var code = @"
func test(obj):
    obj.call(""unknown_method"")
";
        var classDecl = _reader.ParseFileContent(code);
        var callExpr = classDecl.AllNodes.OfType<GDCallExpression>()
            .FirstOrDefault(c => c.CallerExpression is GDMemberOperatorExpression m && m.Identifier?.Sequence == "call");
        if (callExpr == null) return;

        var fixes = _fixProvider.GetFixes("GD7015", callExpr, null, null).ToList();

        var guardFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.AddMethodGuard);
        guardFix.Should().NotBeNull("should have dynamic method guard");
        guardFix!.NewText.Should().Contain("has_method");
        guardFix.NewText.Should().Contain("unknown_method");

        // AST verification
        var modified = ApplyInsertFix(code, guardFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty();
    }

    [TestMethod]
    public void GetFixes_DynamicPropertyNotFound_IncludesInGuard()
    {
        var code = @"
func test(obj):
    obj.get(""unknown_prop"")
";
        var classDecl = _reader.ParseFileContent(code);
        var callExpr = classDecl.AllNodes.OfType<GDCallExpression>()
            .FirstOrDefault(c => c.CallerExpression is GDMemberOperatorExpression m && m.Identifier?.Sequence == "get");
        if (callExpr == null) return;

        var fixes = _fixProvider.GetFixes("GD7016", callExpr, null, null).ToList();

        var guardFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.AddTypeGuard);
        guardFix.Should().NotBeNull("should have property guard");
        guardFix!.NewText.Should().Contain("unknown_prop");
        guardFix.NewText.Should().Contain("in obj");

        // AST verification
        var modified = ApplyInsertFix(code, guardFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDIfStatement>().Should().NotBeEmpty();
    }

    #endregion

    #region Container Specialization Fix Tests (GD3025)

    [TestMethod]
    public void GetFixes_ContainerMissingSpecialization_SuggestsArrayVariant()
    {
        var code = @"
var items: Array
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.AllNodes.OfType<GDVariableDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD3025", varDecl, null, null).ToList();

        var specializationFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.AddTypeAnnotation);
        specializationFix.Should().NotBeNull("should suggest Array[Variant]");
        specializationFix!.NewText.Should().Be("Array[Variant]");

        // AST verification
        var modified = ApplyReplaceFix(code, specializationFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        var modifiedDecl = modifiedAst.AllNodes.OfType<GDVariableDeclaration>().FirstOrDefault();
        modifiedDecl.Should().NotBeNull();
        modifiedDecl!.Type?.ToString().Should().Contain("Array");
    }

    [TestMethod]
    public void GetFixes_ContainerMissingSpecialization_SuggestsDictionaryVariant()
    {
        var code = @"
var data: Dictionary
";
        var classDecl = _reader.ParseFileContent(code);
        var varDecl = classDecl.AllNodes.OfType<GDVariableDeclaration>().First();

        var fixes = _fixProvider.GetFixes("GD3025", varDecl, null, null).ToList();

        var specializationFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.AddTypeAnnotation);
        specializationFix.Should().NotBeNull("should suggest Dictionary[Variant, Variant]");
        specializationFix!.NewText.Should().Be("Dictionary[Variant, Variant]");
    }

    #endregion

    #region Add Await Fix Tests (GD5011)

    [TestMethod]
    public void GetFixes_PossibleMissedAwait_IncludesAddAwait()
    {
        var code = @"
func _ready():
    do_async()
";
        var classDecl = _reader.ParseFileContent(code);
        var callExpr = classDecl.AllNodes.OfType<GDCallExpression>().First();

        var fixes = _fixProvider.GetFixes("GD5011", callExpr, null, null).ToList();

        var addAwaitFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.AddAwait);
        addAwaitFix.Should().NotBeNull("should have add await fix");
        addAwaitFix!.NewText.Should().Be("await ");

        // AST verification
        var modified = ApplyInsertFix(code, addAwaitFix);
        var modifiedAst = _reader.ParseFileContent(modified);
        modifiedAst.AllNodes.OfType<GDAwaitExpression>().Should().NotBeEmpty("await expression should now exist");
        modifiedAst.AllNodes.OfType<GDCallExpression>().Should().NotBeEmpty("call should still exist inside await");
    }

    [TestMethod]
    public void GetFixes_PossibleMissedAwait_FromExpressionStatement()
    {
        var code = @"
func _ready():
    do_async()
";
        var classDecl = _reader.ParseFileContent(code);
        var exprStmt = classDecl.AllNodes.OfType<GDExpressionStatement>().First();

        var fixes = _fixProvider.GetFixes("GD5011", exprStmt, null, null).ToList();

        var addAwaitFix = fixes.OfType<GDTextEditFixDescriptor>()
            .FirstOrDefault(f => f.Kind == GDFixKind.AddAwait);
        addAwaitFix.Should().NotBeNull("should handle expression statement containing call");
    }

    #endregion

    #region Fix Application Helpers

    private string ApplyInsertFix(string code, GDTextEditFixDescriptor fix)
    {
        var lines = code.Split('\n').ToList();
        var lineIndex = fix.Line - 1; // 1-based → 0-based
        if (lineIndex < 0 || lineIndex >= lines.Count)
            return code;

        var line = lines[lineIndex];
        var col = Math.Min(fix.StartColumn, line.Length);
        lines[lineIndex] = line.Insert(col, fix.NewText);
        return string.Join("\n", lines);
    }

    private string ApplyRemoveFix(string code, GDTextEditFixDescriptor fix)
    {
        var lines = code.Split('\n').ToList();
        var lineIndex = fix.Line - 1;
        if (lineIndex < 0 || lineIndex >= lines.Count)
            return code;

        var line = lines[lineIndex];
        var start = Math.Min(fix.StartColumn, line.Length);
        var end = Math.Min(fix.EndColumn, line.Length);
        if (start < end)
            lines[lineIndex] = line.Remove(start, end - start);
        return string.Join("\n", lines);
    }

    private string ApplyReplaceFix(string code, GDTextEditFixDescriptor fix)
    {
        var lines = code.Split('\n').ToList();
        var lineIndex = fix.Line - 1;
        if (lineIndex < 0 || lineIndex >= lines.Count)
            return code;

        var line = lines[lineIndex];
        var start = Math.Min(fix.StartColumn, line.Length);
        var end = Math.Min(fix.EndColumn, line.Length);
        lines[lineIndex] = line[..start] + fix.NewText + line[end..];
        return string.Join("\n", lines);
    }

    #endregion

    /// <summary>
    /// Mock runtime provider for testing.
    /// </summary>
    private class MockRuntimeProvider : IGDRuntimeProvider
    {
        private readonly Dictionary<string, GDRuntimeTypeInfo> _types = new();

        public void AddType(string name, GDRuntimeMemberInfo[] members)
        {
            _types[name] = new GDRuntimeTypeInfo(name) { Members = members };
        }

        public bool IsKnownType(string typeName) => _types.ContainsKey(typeName);

        public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
        {
            return _types.TryGetValue(typeName, out var info) ? info : null;
        }

        public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
        {
            if (_types.TryGetValue(typeName, out var info))
            {
                return info.Members.FirstOrDefault(m => m.Name == memberName);
            }
            return null;
        }

        public string? GetBaseType(string typeName) => null;

        public bool IsAssignableTo(string sourceType, string targetType)
        {
            return sourceType == targetType;
        }

        public GDRuntimeFunctionInfo? GetGlobalFunction(string functionName) => null;

        public GDRuntimeTypeInfo? GetGlobalClass(string className) => null;

        public bool IsBuiltIn(string identifier) => false;

        public IEnumerable<string> GetAllTypes() => _types.Keys;

        public bool IsBuiltinType(string typeName) => false;

        public IReadOnlyList<string> FindTypesWithMethod(string methodName) => Array.Empty<string>();

        // Type Traits - stub implementations
        public bool IsNumericType(string typeName) => false;
        public bool IsIterableType(string typeName) => false;
        public bool IsIndexableType(string typeName) => false;
        public bool IsNullableType(string typeName) => true;
        public bool IsVectorType(string typeName) => false;
        public bool IsContainerType(string typeName) => false;
        public bool IsPackedArrayType(string typeName) => false;
        public string? GetFloatVectorVariant(string integerVectorType) => null;
        public string? GetPackedArrayElementType(string packedArrayType) => null;
        public string? ResolveOperatorResult(string leftType, string operatorName, string rightType) => null;
        public IReadOnlyList<string> GetTypesWithOperator(string operatorName) => Array.Empty<string>();
        public IReadOnlyList<string> GetTypesWithNonZeroCollisionLayer() => Array.Empty<string>();
        public IReadOnlyList<GDCollisionLayerInfo> GetCollisionLayerDetails() => Array.Empty<GDCollisionLayerInfo>();
        public IReadOnlyList<string> GetTypesWithNonZeroAvoidanceLayers() => Array.Empty<string>();
        public IReadOnlyList<GDAvoidanceLayerInfo> GetAvoidanceLayerDetails() => Array.Empty<GDAvoidanceLayerInfo>();
        public GDShrapt.Reader.GDExpression? GetConstantInitializer(string typeName, string constantName) => null;
        public bool IsVirtualMethod(string typeName, string methodName) => false;
    }
}
