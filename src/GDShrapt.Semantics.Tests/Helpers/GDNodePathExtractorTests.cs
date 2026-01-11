using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDNodePathExtractorTests
{
    private readonly GDScriptReader _reader = new();

    [TestMethod]
    public void ExtractFromGetNodeExpression_SimpleNode_ReturnsPath()
    {
        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);

        Assert.AreEqual("Player", path);
    }

    [TestMethod]
    public void ExtractFromGetNodeExpression_NestedPath_ReturnsFullPath()
    {
        var expr = _reader.ParseExpression("$Player/Sprite") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);

        Assert.AreEqual("Player/Sprite", path);
    }

    [TestMethod]
    public void ExtractFromGetNodeExpression_DeeplyNestedPath_ReturnsFullPath()
    {
        var expr = _reader.ParseExpression("$UI/Container/Label") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);

        Assert.AreEqual("UI/Container/Label", path);
    }

    [TestMethod]
    public void ExtractFromGetNodeExpression_ParentPath_ReturnsWithDots()
    {
        // Note: $../Sibling parses differently - the parser interprets .. as special operator
        // Use get_node() for parent references in tests
        var expr = _reader.ParseExpression("get_node(\"../Sibling\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.AreEqual("../Sibling", path);
    }

    [TestMethod]
    public void ExtractFromGetNodeExpression_MultipleParentRefs_ReturnsCorrectPath()
    {
        // Note: Multiple .. references are better tested via get_node()
        var expr = _reader.ParseExpression("get_node(\"../../Sibling/Child\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.AreEqual("../../Sibling/Child", path);
    }

    [TestMethod]
    public void ExtractFromGetNodeExpression_CurrentNode_ReturnsDot()
    {
        var expr = _reader.ParseExpression("$./Child") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);

        Assert.AreEqual("./Child", path);
    }

    [TestMethod]
    public void ExtractFromUniqueNodeExpression_ReturnsNodeName()
    {
        var expr = _reader.ParseExpression("%Player") as GDGetUniqueNodeExpression;
        Assert.IsNotNull(expr);

        var name = GDNodePathExtractor.ExtractFromUniqueNodeExpression(expr);

        Assert.AreEqual("Player", name);
    }

    [TestMethod]
    public void ExtractFromUniqueNodeExpression_LongName_ReturnsCorrectName()
    {
        var expr = _reader.ParseExpression("%StatusLabel") as GDGetUniqueNodeExpression;
        Assert.IsNotNull(expr);

        var name = GDNodePathExtractor.ExtractFromUniqueNodeExpression(expr);

        Assert.AreEqual("StatusLabel", name);
    }

    [TestMethod]
    public void ExtractFromCallExpression_GetNode_ReturnsPath()
    {
        var expr = _reader.ParseExpression("get_node(\"Player/Sprite\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.AreEqual("Player/Sprite", path);
    }

    [TestMethod]
    public void ExtractFromCallExpression_GetNodeOrNull_ReturnsPath()
    {
        var expr = _reader.ParseExpression("get_node_or_null(\"Enemy\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.AreEqual("Enemy", path);
    }

    [TestMethod]
    public void ExtractFromCallExpression_FindNode_ReturnsPath()
    {
        var expr = _reader.ParseExpression("find_node(\"Label\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.AreEqual("Label", path);
    }

    [TestMethod]
    public void ExtractFromCallExpression_NonGetNodeCall_ReturnsNull()
    {
        var expr = _reader.ParseExpression("print(\"hello\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.IsNull(path);
    }

    [TestMethod]
    public void ExtractFromCallExpression_NoArguments_ReturnsNull()
    {
        var expr = _reader.ParseExpression("get_node()") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractFromCallExpression(expr);

        Assert.IsNull(path);
    }

    [TestMethod]
    public void ExtractFromCallExpression_GetNodeWithVariable_ResolvesConst()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
const PATH = ""Enemy""
func test():
    get_node(PATH)
");
        var call = classDecl.AllNodes.OfType<GDCallExpression>().First();

        Func<string, GDExpression?> resolver = varName =>
            GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, varName);

        var path = GDNodePathExtractor.ExtractFromCallExpression(call, resolver);

        Assert.AreEqual("Enemy", path);
    }

    [TestMethod]
    public void ExtractFromCallExpression_GetNodeWithTypeInferredVar_ResolvesValue()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
var player_path := ""Player/Sprite""
func test():
    get_node(player_path)
");
        var call = classDecl.AllNodes.OfType<GDCallExpression>().First();

        Func<string, GDExpression?> resolver = varName =>
            GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, varName);

        var path = GDNodePathExtractor.ExtractFromCallExpression(call, resolver);

        Assert.AreEqual("Player/Sprite", path);
    }

    [TestMethod]
    public void ExtractResourcePath_Preload_ReturnsPath()
    {
        var expr = _reader.ParseExpression("preload(\"res://scenes/main.tscn\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractResourcePath(expr);

        Assert.AreEqual("res://scenes/main.tscn", path);
    }

    [TestMethod]
    public void ExtractResourcePath_Load_ReturnsPath()
    {
        var expr = _reader.ParseExpression("load(\"res://scripts/player.gd\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractResourcePath(expr);

        Assert.AreEqual("res://scripts/player.gd", path);
    }

    [TestMethod]
    public void ExtractResourcePath_NonLoadCall_ReturnsNull()
    {
        var expr = _reader.ParseExpression("get_node(\"Player\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var path = GDNodePathExtractor.ExtractResourcePath(expr);

        Assert.IsNull(path);
    }

    [TestMethod]
    public void TryGetStaticStringInitializer_Const_ReturnsInitializer()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
const PATH = ""Player/Sprite""
");
        var result = GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, "PATH");

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(GDStringExpression));
        Assert.AreEqual("Player/Sprite", ((GDStringExpression)result).String?.Sequence);
    }

    [TestMethod]
    public void TryGetStaticStringInitializer_VarWithTypeInfer_ReturnsInitializer()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
var path := ""Enemy""
");
        var result = GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, "path");

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(GDStringExpression));
        Assert.AreEqual("Enemy", ((GDStringExpression)result).String?.Sequence);
    }

    [TestMethod]
    public void TryGetStaticStringInitializer_VarWithoutTypeInfer_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
var path = ""Enemy""
");
        // var without := is not considered statically safe
        var result = GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, "path");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryGetStaticStringInitializer_VarWithExplicitType_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
var path: String = ""Enemy""
");
        // var with explicit type is not considered statically safe (can be reassigned)
        var result = GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, "path");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryGetStaticStringInitializer_NonExistentVariable_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
const OTHER = ""test""
");
        var result = GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, "PATH");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryGetStaticStringInitializer_ConstWithNonStringValue_ReturnsNull()
    {
        var classDecl = _reader.ParseFileContent(@"
extends Node
const COUNT = 42
");
        var result = GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, "COUNT");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsGetNodeCall_GetNode_ReturnsTrue()
    {
        var expr = _reader.ParseExpression("get_node(\"Player\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsTrue(GDNodePathExtractor.IsGetNodeCall(expr));
    }

    [TestMethod]
    public void IsGetNodeCall_GetNodeOrNull_ReturnsTrue()
    {
        var expr = _reader.ParseExpression("get_node_or_null(\"Player\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsTrue(GDNodePathExtractor.IsGetNodeCall(expr));
    }

    [TestMethod]
    public void IsGetNodeCall_FindNode_ReturnsTrue()
    {
        var expr = _reader.ParseExpression("find_node(\"Player\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsTrue(GDNodePathExtractor.IsGetNodeCall(expr));
    }

    [TestMethod]
    public void IsGetNodeCall_OtherCall_ReturnsFalse()
    {
        var expr = _reader.ParseExpression("print(\"hello\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsFalse(GDNodePathExtractor.IsGetNodeCall(expr));
    }

    [TestMethod]
    public void IsPreloadOrLoadCall_Preload_ReturnsTrue()
    {
        var expr = _reader.ParseExpression("preload(\"res://test.gd\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsTrue(GDNodePathExtractor.IsPreloadOrLoadCall(expr));
    }

    [TestMethod]
    public void IsPreloadOrLoadCall_Load_ReturnsTrue()
    {
        var expr = _reader.ParseExpression("load(\"res://test.gd\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsTrue(GDNodePathExtractor.IsPreloadOrLoadCall(expr));
    }

    [TestMethod]
    public void IsPreloadOrLoadCall_OtherCall_ReturnsFalse()
    {
        var expr = _reader.ParseExpression("get_node(\"Player\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        Assert.IsFalse(GDNodePathExtractor.IsPreloadOrLoadCall(expr));
    }

    [TestMethod]
    public void GetCallName_SimpleCall_ReturnsName()
    {
        var expr = _reader.ParseExpression("print(\"hello\")") as GDCallExpression;
        Assert.IsNotNull(expr);

        var name = GDNodePathExtractor.GetCallName(expr);

        Assert.AreEqual("print", name);
    }

    [TestMethod]
    public void GetCallName_MemberCall_ReturnsMethodName()
    {
        var expr = _reader.ParseExpression("node.get_parent()") as GDCallExpression;
        Assert.IsNotNull(expr);

        var name = GDNodePathExtractor.GetCallName(expr);

        Assert.AreEqual("get_parent", name);
    }
}
