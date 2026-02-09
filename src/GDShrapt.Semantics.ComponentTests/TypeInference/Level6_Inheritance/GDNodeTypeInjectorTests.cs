using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class GDNodeTypeInjectorTests
{
    private readonly GDScriptReader _reader = new();

    #region Mock Implementations

    private class MockFileSystem : IGDFileSystem
    {
        private readonly Dictionary<string, string> _files = new();

        public void AddFile(string path, string content)
        {
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            _files[normalizedPath] = content;
        }

        private string NormalizePath(string path) =>
            path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));
        public bool DirectoryExists(string path) => true;
        public string ReadAllText(string path) => _files[NormalizePath(path)];
        public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive) =>
            _files.Keys.Where(k => k.EndsWith(pattern.TrimStart('*')));
        public IEnumerable<string> GetDirectories(string directory) => new string[0];
        public string GetFullPath(string path) => NormalizePath(path);
        public string CombinePath(params string[] paths) => Path.Combine(paths);
        public string GetFileName(string path) => Path.GetFileName(path);
        public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
        public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
        public string GetExtension(string path) => Path.GetExtension(path);
    }

    private class MockScriptInfo : IGDScriptInfo
    {
        public string? TypeName { get; set; }
        public string? FullPath { get; set; }
        public string? ResPath { get; set; }
        public GDClassDeclaration? Class { get; set; }
        public bool IsGlobal { get; set; }
    }

    private class MockScriptProvider : IGDScriptProvider
    {
        private readonly List<IGDScriptInfo> _scripts = new();

        public void AddScript(string path, string? typeName)
        {
            _scripts.Add(new MockScriptInfo { FullPath = path, TypeName = typeName });
        }

        public IEnumerable<IGDScriptInfo> Scripts => _scripts;

        public IGDScriptInfo? GetScriptByTypeName(string typeName) =>
            _scripts.FirstOrDefault(s => s.TypeName == typeName);

        public IGDScriptInfo? GetScriptByPath(string path) =>
            _scripts.FirstOrDefault(s => s.FullPath == path || s.FullPath?.EndsWith(path.Replace("res://", "")) == true);
    }

    #endregion

    [TestMethod]
    public void InjectType_GetNodeExpression_ReturnsNodeTypeFromScene()
    {
        // Arrange - create scene with Player node of type CharacterBody2D
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]

[node name=""Main"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://main.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        // Create $Player expression
        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Use resource path for ScriptPath (as scene stores res:// paths)
        var context = new GDTypeInjectionContext { ScriptPath = "res://player.gd" };

        // Act
        var type = injector.InjectType(expr, context);

        // Assert
        Assert.AreEqual("CharacterBody2D", type);
    }

    [TestMethod]
    public void InjectType_GetNodeExpression_NoSceneProvider_ReturnsNull()
    {
        var injector = new GDNodeTypeInjector(sceneProvider: null);

        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var context = new GDTypeInjectionContext { ScriptPath = "/project/player.gd" };

        var type = injector.InjectType(expr, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_GetNodeExpression_ScriptNotInScene_ReturnsNull()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Main"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://main.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        // Script path that is not in any scene
        var context = new GDTypeInjectionContext { ScriptPath = "res://unknown.gd" };

        var type = injector.InjectType(expr, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_PreloadScene_ReturnsPackedScene()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://main.tscn\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("PackedScene", type);
    }

    [TestMethod]
    public void InjectType_PreloadScnFile_ReturnsPackedScene()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://level.scn\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("PackedScene", type);
    }

    [TestMethod]
    public void InjectType_PreloadScript_WithClassName_ReturnsClassName()
    {
        var mockScriptProvider = new MockScriptProvider();
        mockScriptProvider.AddScript("/project/player.gd", "Player");

        var injector = new GDNodeTypeInjector(scriptProvider: mockScriptProvider);

        var call = _reader.ParseExpression("preload(\"res://player.gd\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Player", type);
    }

    [TestMethod]
    public void InjectType_PreloadScript_NoClassName_ReturnsGDScript()
    {
        var mockScriptProvider = new MockScriptProvider();
        mockScriptProvider.AddScript("/project/utils.gd", null); // No class_name

        var injector = new GDNodeTypeInjector(scriptProvider: mockScriptProvider);

        var call = _reader.ParseExpression("preload(\"res://utils.gd\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("GDScript", type);
    }

    [TestMethod]
    public void InjectType_PreloadResource_ReturnResource()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://data.tres\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Resource", type);
    }

    [TestMethod]
    public void InjectType_PreloadResFile_ReturnResource()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://data.res\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Resource", type);
    }

    [TestMethod]
    public void InjectType_PreloadPng_ReturnsTexture2D()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://icon.png\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Texture2D", type);
    }

    [TestMethod]
    public void InjectType_PreloadJpg_ReturnsTexture2D()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://photo.jpg\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Texture2D", type);
    }

    [TestMethod]
    public void InjectType_PreloadWav_ReturnsAudioStream()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://sound.wav\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("AudioStream", type);
    }

    [TestMethod]
    public void InjectType_PreloadOgg_ReturnsAudioStream()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://music.ogg\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("AudioStream", type);
    }

    [TestMethod]
    public void InjectType_PreloadFont_ReturnsFont()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://font.ttf\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Font", type);
    }

    [TestMethod]
    public void InjectType_PreloadJson_ReturnsJSON()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://data.json\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("JSON", type);
    }

    [TestMethod]
    public void InjectType_PreloadGlb_ReturnsPackedScene()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://model.glb\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("PackedScene", type);
    }

    [TestMethod]
    public void InjectType_Load_SameAsPreload()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("load(\"res://main.tscn\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("PackedScene", type);
    }

    [TestMethod]
    public void InjectType_UniqueNode_ReturnsNodeTypeFromScene()
    {
        // Scene with unique node
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://script.gd"" id=""1""]

[node name=""Main"" type=""Control""]
script = ExtResource(""1"")

[node name=""StatusLabel"" type=""Label"" parent="".""]
unique_name_in_owner = true
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://main.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var expr = _reader.ParseExpression("%StatusLabel") as GDGetUniqueNodeExpression;
        Assert.IsNotNull(expr);

        var context = new GDTypeInjectionContext { ScriptPath = "res://script.gd" };

        var type = injector.InjectType(expr, context);

        Assert.AreEqual("Label", type);
    }

    [TestMethod]
    public void InjectType_GetNodeCall_StringLiteral_ReturnsNodeType()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://game.gd"" id=""1""]

[node name=""Game"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Enemy"" type=""CharacterBody2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://main.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var call = _reader.ParseExpression("get_node(\"Enemy\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://game.gd" };

        var type = injector.InjectType(call, context);

        Assert.AreEqual("CharacterBody2D", type);
    }

    [TestMethod]
    public void InjectType_NonNodeExpression_ReturnsNull()
    {
        var injector = new GDNodeTypeInjector();

        var expr = _reader.ParseExpression("10 + 20") as GDDualOperatorExpression;
        Assert.IsNotNull(expr);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(expr, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_PrintCall_ReturnsNull()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("print(\"hello\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_NestedNodePath_ReturnsCorrectType()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://main.gd"" id=""1""]

[node name=""Main"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""UI"" type=""Control"" parent="".""]

[node name=""StatusLabel"" type=""Label"" parent=""UI""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://main.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var expr = _reader.ParseExpression("$UI/StatusLabel") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var context = new GDTypeInjectionContext { ScriptPath = "res://main.gd" };

        var type = injector.InjectType(expr, context);

        Assert.AreEqual("Label", type);
    }

    [TestMethod]
    public void InjectType_NodeWithScript_ReturnsScriptTypeName()
    {
        // When a node has a script, the scene provider infers a type name from the script path
        // e.g., "res://player.gd" -> "Player" (PascalCase from filename)
        var sceneContent = @"
[gd_scene load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://main.gd"" id=""1""]
[ext_resource type=""Script"" path=""res://player.gd"" id=""2""]

[node name=""Main"" type=""Node2D""]
script = ExtResource(""1"")

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
script = ExtResource(""2"")
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://main.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var expr = _reader.ParseExpression("$Player") as GDGetNodeExpression;
        Assert.IsNotNull(expr);

        var context = new GDTypeInjectionContext { ScriptPath = "res://main.gd" };

        var type = injector.InjectType(expr, context);

        // GDSceneTypesProvider infers "Player" from script path "res://player.gd"
        Assert.AreEqual("Player", type);
    }

    [TestMethod]
    public void InjectType_GetNodeOrNull_ReturnsNodeType()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://test.gd"" id=""1""]

[node name=""Root"" type=""Node""]
script = ExtResource(""1"")

[node name=""Child"" type=""Sprite2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scene.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://scene.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var call = _reader.ParseExpression("get_node_or_null(\"Child\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Sprite2D", type);
    }

    [TestMethod]
    public void InjectType_UnknownResourceExtension_ReturnsResource()
    {
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("preload(\"res://unknown.xyz\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext();

        var type = injector.InjectType(call, context);

        Assert.AreEqual("Resource", type);
    }

    #region GetSignalParameterTypes Tests

    [TestMethod]
    public void GetSignalParameterTypes_GodotSignal_Timeout_ReturnsEmpty()
    {
        // Timer.timeout has no parameters
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("timeout", "Timer");

        Assert.IsNotNull(paramTypes);
        Assert.AreEqual(0, paramTypes.Count, "timeout signal should have no parameters");
    }

    [TestMethod]
    public void GetSignalParameterTypes_GodotSignal_ChildEnteredTree_ReturnsNode()
    {
        // Node.child_entered_tree(node: Node)
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("child_entered_tree", "Node");

        Assert.IsNotNull(paramTypes);
        Assert.AreEqual(1, paramTypes.Count, "child_entered_tree should have 1 parameter");
        Assert.AreEqual("Node", paramTypes[0]);
    }

    [TestMethod]
    public void GetSignalParameterTypes_GodotSignal_TreeEntered_ReturnsEmpty()
    {
        // Node.tree_entered has no parameters
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("tree_entered", "Node");

        Assert.IsNotNull(paramTypes);
        Assert.AreEqual(0, paramTypes.Count, "tree_entered signal should have no parameters");
    }

    [TestMethod]
    public void GetSignalParameterTypes_UnknownSignal_ReturnsNull()
    {
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("unknown_signal", "Node");

        Assert.IsNull(paramTypes);
    }

    [TestMethod]
    public void GetSignalParameterTypes_UnknownType_ReturnsNull()
    {
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("timeout", "UnknownType");

        Assert.IsNull(paramTypes);
    }

    [TestMethod]
    public void GetSignalParameterTypes_NoGodotProvider_ReturnsNull()
    {
        var injector = new GDNodeTypeInjector();

        var paramTypes = injector.GetSignalParameterTypes("timeout", "Timer");

        Assert.IsNull(paramTypes);
    }

    [TestMethod]
    public void GetSignalParameterTypes_NullEmitterType_ReturnsNull()
    {
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("timeout", null);

        Assert.IsNull(paramTypes);
    }

    [TestMethod]
    public void GetSignalParameterTypes_EmptyEmitterType_ReturnsNull()
    {
        var godotProvider = new GDGodotTypesProvider();
        var injector = new GDNodeTypeInjector(godotTypesProvider: godotProvider);

        var paramTypes = injector.GetSignalParameterTypes("timeout", "");

        Assert.IsNull(paramTypes);
    }

    [TestMethod]
    public void GetSignalParameterTypes_ProjectScript_ReturnsSignalParams()
    {
        // Create a mock script provider with a script that has a signal
        var mockScriptProvider = new MockScriptProviderWithSignal();

        // Parse a script with a signal declaration
        var code = @"
extends Node
signal health_changed(new_value: int)
";
        var classDecl = _reader.ParseFileContent(code);
        mockScriptProvider.AddScript("TestScript", classDecl);

        var injector = new GDNodeTypeInjector(
            scriptProvider: mockScriptProvider,
            godotTypesProvider: new GDGodotTypesProvider());

        var paramTypes = injector.GetSignalParameterTypes("health_changed", "TestScript");

        Assert.IsNotNull(paramTypes);
        Assert.AreEqual(1, paramTypes.Count, "health_changed should have 1 parameter");
        Assert.AreEqual("int", paramTypes[0]);
    }

    [TestMethod]
    public void GetSignalParameterTypes_ProjectScript_NoParams_ReturnsEmpty()
    {
        var mockScriptProvider = new MockScriptProviderWithSignal();

        var code = @"
extends Node
signal ready_custom()
";
        var classDecl = _reader.ParseFileContent(code);
        mockScriptProvider.AddScript("TestScript", classDecl);

        var injector = new GDNodeTypeInjector(
            scriptProvider: mockScriptProvider,
            godotTypesProvider: new GDGodotTypesProvider());

        var paramTypes = injector.GetSignalParameterTypes("ready_custom", "TestScript");

        Assert.IsNotNull(paramTypes);
        Assert.AreEqual(0, paramTypes.Count, "ready_custom should have no parameters");
    }

    [TestMethod]
    public void GetSignalParameterTypes_ProjectScript_MultipleParams_ReturnsAll()
    {
        var mockScriptProvider = new MockScriptProviderWithSignal();

        var code = @"
extends Node
signal position_changed(x: float, y: float)
";
        var classDecl = _reader.ParseFileContent(code);
        mockScriptProvider.AddScript("TestScript", classDecl);

        var injector = new GDNodeTypeInjector(
            scriptProvider: mockScriptProvider,
            godotTypesProvider: new GDGodotTypesProvider());

        var paramTypes = injector.GetSignalParameterTypes("position_changed", "TestScript");

        Assert.IsNotNull(paramTypes);
        Assert.AreEqual(2, paramTypes.Count, "position_changed should have 2 parameters");
        Assert.AreEqual("float", paramTypes[0]);
        Assert.AreEqual("float", paramTypes[1]);
    }

    private class MockScriptProviderWithSignal : IGDScriptProvider
    {
        private readonly Dictionary<string, IGDScriptInfo> _scripts = new();

        public void AddScript(string typeName, GDClassDeclaration classDecl)
        {
            _scripts[typeName] = new MockScriptInfoWithClass
            {
                TypeName = typeName,
                Class = classDecl
            };
        }

        public IEnumerable<IGDScriptInfo> Scripts => _scripts.Values;

        public IGDScriptInfo? GetScriptByTypeName(string typeName) =>
            _scripts.TryGetValue(typeName, out var script) ? script : null;

        public IGDScriptInfo? GetScriptByPath(string path) => null;
    }

    private class MockScriptInfoWithClass : IGDScriptInfo
    {
        public string? TypeName { get; set; }
        public string? FullPath { get; set; }
        public string? ResPath { get; set; }
        public GDClassDeclaration? Class { get; set; }
        public bool IsGlobal { get; set; }
    }

    #endregion

    #region Scene-Aware Instantiate Tests

    [TestMethod]
    public void InjectType_PreloadInstantiate_ReturnsRootNodeType()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Player"" type=""CharacterBody2D""]
[node name=""CollisionShape"" type=""CollisionShape2D"" parent="".""]
[node name=""Sprite"" type=""Sprite2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "player.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://player.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var call = _reader.ParseExpression("preload(\"res://player.tscn\").instantiate()") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("CharacterBody2D", type);
    }

    [TestMethod]
    public void InjectType_PreloadInstantiate_WithScript_ReturnsScriptType()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]
[ext_resource type=""Script"" path=""res://player.gd"" id=""1""]
[node name=""Player"" type=""CharacterBody2D""]
script = ExtResource(""1"")
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "player.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://player.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var call = _reader.ParseExpression("preload(\"res://player.tscn\").instantiate()") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("Player", type);
    }

    [TestMethod]
    public void InjectType_LoadInstantiate_ReturnsRootNodeType()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Enemy"" type=""Node2D""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "enemy.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://enemy.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var call = _reader.ParseExpression("load(\"res://enemy.tscn\").instantiate()") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("Node2D", type);
    }

    [TestMethod]
    public void InjectType_VariableInstantiate_ReturnsRootType()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Enemy"" type=""Node2D""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "enemy.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://enemy.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var code = @"
var enemy_scene = preload(""res://enemy.tscn"")

func test():
    var instance = enemy_scene.instantiate()
";
        var classDecl = _reader.ParseFileContent(code);
        Assert.IsNotNull(classDecl);

        var instantiateCall = FindCallExpression(classDecl, "instantiate");
        Assert.IsNotNull(instantiateCall, "Should find instantiate() call in AST");

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(instantiateCall, context);

        Assert.AreEqual("Node2D", type);
    }

    [TestMethod]
    public void InjectType_NonSceneInstantiate_ReturnsNull()
    {
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        var injector = new GDNodeTypeInjector(sceneProvider);

        var call = _reader.ParseExpression("preload(\"res://texture.png\").instantiate()") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_GetChildOnSceneInstance_ReturnsChildType()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Root"" type=""Node2D""]
[node name=""Sprite"" type=""Sprite2D"" parent="".""]
[node name=""Collision"" type=""CollisionShape2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "entity.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://entity.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var code = @"
var entity = preload(""res://entity.tscn"").instantiate()

func test():
    var child0 = entity.get_child(0)
    var child1 = entity.get_child(1)
";
        var classDecl = _reader.ParseFileContent(code);
        Assert.IsNotNull(classDecl);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };

        var getChild0 = FindCallExpressionWithArg(classDecl, "get_child", "0");
        Assert.IsNotNull(getChild0, "Should find get_child(0) in AST");
        var type0 = injector.InjectType(getChild0, context);
        Assert.AreEqual("Sprite2D", type0);

        var getChild1 = FindCallExpressionWithArg(classDecl, "get_child", "1");
        Assert.IsNotNull(getChild1, "Should find get_child(1) in AST");
        var type1 = injector.InjectType(getChild1, context);
        Assert.AreEqual("CollisionShape2D", type1);
    }

    [TestMethod]
    public void InjectType_GetChildOutOfBounds_ReturnsNull()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Root"" type=""Node2D""]
[node name=""OnlyChild"" type=""Sprite2D"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "small.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://small.tscn");

        var injector = new GDNodeTypeInjector(sceneProvider);

        var code = @"
var node = preload(""res://small.tscn"").instantiate()

func test():
    var child = node.get_child(99)
";
        var classDecl = _reader.ParseFileContent(code);
        Assert.IsNotNull(classDecl);

        var getChild = FindCallExpression(classDecl, "get_child");
        Assert.IsNotNull(getChild);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(getChild, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void GetRootNodeType_ReturnsFirstNodeType()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Main"" type=""Control""]
[node name=""Child"" type=""Button"" parent="".""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "ui.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://ui.tscn");

        Assert.AreEqual("Control", provider.GetRootNodeType("res://ui.tscn"));
    }

    [TestMethod]
    public void GetDirectChildren_ReturnsImmediateChildren()
    {
        var sceneContent = @"
[gd_scene format=3]
[node name=""Root"" type=""Node2D""]
[node name=""Child1"" type=""Sprite2D"" parent="".""]
[node name=""Child2"" type=""CollisionShape2D"" parent="".""]
[node name=""GrandChild"" type=""Label"" parent=""Child1""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "test.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://test.tscn");

        var children = provider.GetDirectChildren("res://test.tscn", ".");
        Assert.AreEqual(2, children.Count);
        Assert.AreEqual("Sprite2D", children[0].NodeType);
        Assert.AreEqual("CollisionShape2D", children[1].NodeType);
    }

    private static GDCallExpression? FindCallExpression(GDClassDeclaration classDecl, string methodName)
    {
        GDCallExpression? found = null;
        classDecl.WalkIn(new CallExpressionFinder(methodName, null, e => found = e));
        return found;
    }

    private static GDCallExpression? FindCallExpressionWithArg(GDClassDeclaration classDecl, string methodName, string argValue)
    {
        GDCallExpression? found = null;
        classDecl.WalkIn(new CallExpressionFinder(methodName, argValue, e => found = e));
        return found;
    }

    private class CallExpressionFinder : GDVisitor
    {
        private readonly string _methodName;
        private readonly string? _argValue;
        private readonly System.Action<GDCallExpression> _onFound;

        public CallExpressionFinder(string methodName, string? argValue, System.Action<GDCallExpression> onFound)
        {
            _methodName = methodName;
            _argValue = argValue;
            _onFound = onFound;
        }

        public override void Visit(GDCallExpression e)
        {
            var name = GDNodePathExtractor.GetCallName(e);
            if (name == _methodName)
            {
                if (_argValue == null)
                {
                    _onFound(e);
                }
                else
                {
                    var args = e.Parameters?.ToList();
                    if (args != null && args.Count > 0 && args[0] is GDNumberExpression num && num.Number?.Sequence == _argValue)
                    {
                        _onFound(e);
                    }
                }
            }
            base.Visit(e);
        }
    }

    #endregion
}
