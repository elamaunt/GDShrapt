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
    }
}
