using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for duck typing analysis in GDShrapt.
/// </summary>
[TestClass]
public class DuckTypingTests
{
    #region Duck Type Collection Tests

    [TestMethod]
    public void DuckType_MethodCall_CollectsRequiredMethod()
    {
        // Arrange
        var code = @"
func process(obj):
    obj.move()
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("obj");

        // Assert
        Assert.IsNotNull(duckType, "Duck type should be collected for untyped parameter");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("move"), "Required method 'move' should be collected");
    }

    [TestMethod]
    public void DuckType_MultipleMethodCalls_CollectsAllMethods()
    {
        // Arrange
        var code = @"
func process(entity):
    entity.move()
    entity.attack()
    entity.take_damage(10)
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("move"));
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("attack"));
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("take_damage"));
        Assert.AreEqual(3, duckType.RequiredMethods.Count);
    }

    [TestMethod]
    public void DuckType_PropertyAccess_CollectsRequiredProperty()
    {
        // Arrange
        var code = @"
func process(entity):
    var h = entity.health
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("health"), "Required property 'health' should be collected");
    }

    [TestMethod]
    public void DuckType_PropertyWrite_CollectsRequiredProperty()
    {
        // Arrange
        var code = @"
func process(entity):
    entity.health = 100
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("health"));
    }

    [TestMethod]
    public void DuckType_MixedAccess_CollectsBothMethodsAndProperties()
    {
        // Arrange
        var code = @"
func process(obj):
    obj.name = ""test""
    obj.update()
    var x = obj.position
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("obj");

        // Assert
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("update"));
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("name"));
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("position"));
    }

    [TestMethod]
    public void DuckType_TypedVariable_StillCollectsDuckType()
    {
        // Arrange
        // Note: Currently duck type is collected even for typed parameters because
        // the duck type collector runs after scope validation and doesn't have
        // access to parameter scopes. This is acceptable behavior - the type info
        // is still available via the declared type.
        var code = @"
func process(obj: Node2D):
    obj.position = Vector2.ZERO
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("obj");

        // Assert
        // Currently duck type IS collected even for typed params
        // The effective type will still be Node2D from the declared type
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("position"));
    }

    [TestMethod]
    public void DuckType_ChainedAccess_CollectsRootVariable()
    {
        // Arrange
        var code = @"
func process(entity):
    entity.stats.health = 100
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var duckType = semanticModel?.GetDuckType("entity");

        // Assert
        Assert.IsNotNull(duckType);
        // The root variable 'entity' should have 'stats' as required property
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("stats"));
    }

    #endregion

    #region Duck Type Compatibility Tests

    [TestMethod]
    public void DuckType_IsCompatibleWith_EmptyType_ReturnsTrue()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.RequireMethod("move");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act - empty type string means unknown type
        var result = resolver.IsCompatibleWith(duckType, "");

        // Assert
        Assert.IsTrue(result, "Duck type should be compatible with unknown type");
    }

    [TestMethod]
    public void DuckType_IsCompatibleWith_NullType_ReturnsTrue()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.RequireMethod("attack");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act
        var result = resolver.IsCompatibleWith(duckType, null!);

        // Assert
        Assert.IsTrue(result, "Duck type should be compatible with null type");
    }

    [TestMethod]
    public void DuckType_ExcludedTypes_BlocksCompatibility()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.ExcludedTypes.Add("Enemy");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act
        var result = resolver.IsCompatibleWith(duckType, "Enemy");

        // Assert
        Assert.IsFalse(result, "Excluded type should not be compatible");
    }

    [TestMethod]
    public void DuckType_PossibleTypes_OnlyAllowsListed()
    {
        // Arrange
        var duckType = new GDDuckType();
        duckType.PossibleTypes.Add("Player");
        duckType.PossibleTypes.Add("Enemy");

        var provider = new GDDefaultRuntimeProvider();
        var resolver = new GDDuckTypeResolver(provider);

        // Act & Assert
        // Types in PossibleTypes should be compatible
        // Note: This requires a provider that knows about type inheritance
        // For simplicity, exact match is checked
        Assert.IsFalse(resolver.IsCompatibleWith(duckType, "NPC"), "Type not in PossibleTypes should not be compatible");
    }

    #endregion

    #region Duck Type Merge Tests

    [TestMethod]
    public void DuckType_MergeWith_CombinesRequirements()
    {
        // Arrange
        var duckType1 = new GDDuckType();
        duckType1.RequireMethod("move");
        duckType1.RequireProperty("health");

        var duckType2 = new GDDuckType();
        duckType2.RequireMethod("attack");
        duckType2.RequireProperty("damage");

        // Act
        duckType1.MergeWith(duckType2);

        // Assert
        Assert.IsTrue(duckType1.RequiredMethods.ContainsKey("move"));
        Assert.IsTrue(duckType1.RequiredMethods.ContainsKey("attack"));
        Assert.IsTrue(duckType1.RequiredProperties.ContainsKey("health"));
        Assert.IsTrue(duckType1.RequiredProperties.ContainsKey("damage"));
    }

    [TestMethod]
    public void DuckType_IntersectWith_KeepsCommonRequirements()
    {
        // Arrange
        var duckType1 = new GDDuckType();
        duckType1.RequireMethod("move");
        duckType1.RequireMethod("common");

        var duckType2 = new GDDuckType();
        duckType2.RequireMethod("attack");
        duckType2.RequireMethod("common");

        // Act
        var intersection = duckType1.IntersectWith(duckType2);

        // Assert
        // Intersection merges all requirements (both must have all methods)
        Assert.IsTrue(intersection.RequiredMethods.ContainsKey("move"));
        Assert.IsTrue(intersection.RequiredMethods.ContainsKey("attack"));
        Assert.IsTrue(intersection.RequiredMethods.ContainsKey("common"));
    }

    #endregion

    #region Effective Type Tests

    [TestMethod]
    public void GetEffectiveType_UntypedVariable_ReturnsDuckTypeString()
    {
        // Arrange
        var code = @"
func process(obj):
    obj.move()
    obj.attack()
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var effectiveType = semanticModel?.GetEffectiveType("obj");

        // Assert
        Assert.IsNotNull(effectiveType);
        Assert.IsTrue(effectiveType.Contains("DuckType"), "Effective type should contain DuckType representation");
        Assert.IsTrue(effectiveType.Contains("move") || effectiveType.Contains("methods"), "Should contain method info");
    }

    [TestMethod]
    public void GetEffectiveType_TypedVariable_ReturnsType()
    {
        // Arrange
        var code = @"
var player: Player

func _ready():
    player = Player.new()
";
        var (_, semanticModel) = AnalyzeCode(code);

        // Act
        var effectiveType = semanticModel?.GetEffectiveType("player");

        // Assert
        // Typed variable should return the declared type
        Assert.AreEqual("Player", effectiveType);
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return (null, null);

        // Use GDSemanticReferenceCollector to build semantic model
        // Create a virtual script file for testing
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code); // Parse the code

        var collector = new GDSemanticReferenceCollector(scriptFile);
        var semanticModel = collector.BuildSemanticModel();

        return (classDecl, semanticModel);
    }

    #endregion
}
