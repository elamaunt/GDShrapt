using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class GDResourceFlowTests
{
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
        public string GetFileNameWithoutExtension(string path) =>
            Path.GetFileNameWithoutExtension(path);
        public string? GetDirectoryName(string path) =>
            Path.GetDirectoryName(path);
        public string GetExtension(string path) =>
            Path.GetExtension(path);
    }

    #region Scene Resource Parsing

    [TestMethod]
    public void SceneResourceParsing_TextureExtResource_IsTracked()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Texture2D"" path=""res://icon.png"" id=""1_tex""]

[node name=""Sprite"" type=""Sprite2D""]
texture = ExtResource(""1_tex"")
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "main.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/main.tscn");

        var sceneInfo = provider.GetSceneInfo("res://scenes/main.tscn");
        Assert.IsNotNull(sceneInfo);

        // Check ext_resource is tracked
        Assert.IsTrue(sceneInfo.AllExtResources.ContainsKey("1_tex"));
        Assert.AreEqual("res://icon.png", sceneInfo.AllExtResources["1_tex"].Path);
        Assert.AreEqual("Texture2D", sceneInfo.AllExtResources["1_tex"].Type);

        // Check property assignment is tracked
        Assert.AreEqual(1, sceneInfo.ResourceReferences.Count);
        Assert.AreEqual("res://icon.png", sceneInfo.ResourceReferences[0].ResourcePath);
        Assert.AreEqual("Texture2D", sceneInfo.ResourceReferences[0].ResourceType);
        Assert.AreEqual("texture", sceneInfo.ResourceReferences[0].PropertyName);
        Assert.AreEqual(".", sceneInfo.ResourceReferences[0].NodePath);
    }

    [TestMethod]
    public void SceneResourceParsing_AudioExtResource_IsTracked()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""AudioStream"" path=""res://audio/click.ogg"" id=""1_snd""]

[node name=""Player"" type=""AudioStreamPlayer""]
stream = ExtResource(""1_snd"")
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "audio.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/audio.tscn");

        var refs = provider.GetSceneResourceReferences("res://scenes/audio.tscn");
        Assert.AreEqual(1, refs.Count);
        Assert.AreEqual("res://audio/click.ogg", refs[0].ResourcePath);
        Assert.AreEqual("AudioStream", refs[0].ResourceType);
        Assert.AreEqual("stream", refs[0].PropertyName);
    }

    [TestMethod]
    public void SceneResourceParsing_MultipleResources_AllTracked()
    {
        var sceneContent = @"
[gd_scene load_steps=4 format=3]

[ext_resource type=""Texture2D"" path=""res://icon.png"" id=""1_tex""]
[ext_resource type=""AudioStream"" path=""res://click.ogg"" id=""2_snd""]
[ext_resource type=""Font"" path=""res://font.ttf"" id=""3_fnt""]

[node name=""Main"" type=""Node2D""]

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
texture = ExtResource(""1_tex"")

[node name=""Audio"" type=""AudioStreamPlayer"" parent="".""]
stream = ExtResource(""2_snd"")
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://main.tscn");

        var sceneInfo = provider.GetSceneInfo("res://main.tscn");
        Assert.IsNotNull(sceneInfo);

        // All 3 ext_resources tracked (excluding scripts and scenes)
        Assert.AreEqual(3, sceneInfo.AllExtResources.Count);

        // 2 property assignments tracked (font not bound to a property)
        Assert.AreEqual(2, sceneInfo.ResourceReferences.Count);
        Assert.IsTrue(sceneInfo.ResourceReferences.Any(r => r.PropertyName == "texture"));
        Assert.IsTrue(sceneInfo.ResourceReferences.Any(r => r.PropertyName == "stream"));
    }

    [TestMethod]
    public void SceneResourceParsing_NoResources_EmptyList()
    {
        var sceneContent = @"
[gd_scene load_steps=1 format=3]

[node name=""Main"" type=""Node2D""]
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "main.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://main.tscn");

        var refs = provider.GetSceneResourceReferences("res://main.tscn");
        Assert.AreEqual(0, refs.Count);
    }

    [TestMethod]
    public void SceneResourceParsing_PropertyAssignment_HasNodePath()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Texture2D"" path=""res://icon.png"" id=""1_tex""]

[node name=""Root"" type=""Node2D""]

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
texture = ExtResource(""1_tex"")
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "test.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://test.tscn");

        var refs = provider.GetSceneResourceReferences("res://test.tscn");
        Assert.AreEqual(1, refs.Count);
        Assert.AreEqual("Sprite", refs[0].NodePath);
        Assert.AreEqual("texture", refs[0].PropertyName);
        Assert.AreEqual("1_tex", refs[0].ExtResourceId);
    }

    #endregion

    #region Graph Building

    [TestMethod]
    public void GraphBuild_SceneResources_HasCorrectEdges()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        var edges = graph.GetResourcesUsedBy("res://scenes/level.tscn");
        Assert.IsTrue(edges.Count >= 1);

        var texEdge = edges.FirstOrDefault(e => e.ResourcePath == "res://icon.png");
        Assert.IsNotNull(texEdge);
        Assert.AreEqual(GDResourceReferenceSource.SceneNodeProperty, texEdge.Source);
        Assert.AreEqual(GDTypeConfidence.Certain, texEdge.Confidence);
        Assert.AreEqual("texture", texEdge.PropertyName);
    }

    [TestMethod]
    public void GraphBuild_GetResourceUsages_ReturnsConsumers()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        var usages = graph.GetResourceUsages("res://icon.png");
        Assert.IsTrue(usages.Count >= 1);
        Assert.IsTrue(usages.Any(e => e.ConsumerPath == "res://scenes/level.tscn"));
    }

    [TestMethod]
    public void GraphBuild_GetResourcesUsedBy_ReturnsResources()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        var resources = graph.GetResourcesUsedBy("res://scenes/level.tscn");
        Assert.IsTrue(resources.Count >= 1);
        Assert.IsTrue(resources.Any(e => e.ResourcePath == "res://icon.png"));
    }

    [TestMethod]
    public void GraphBuild_UnboundExtResource_TrackedAsSceneExtResource()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        // The font ext_resource is not bound to any node property
        var fontEdges = graph.GetResourceUsages("res://font.ttf");
        Assert.IsTrue(fontEdges.Count >= 1);

        var fontEdge = fontEdges.FirstOrDefault(e => e.ConsumerPath == "res://scenes/level.tscn");
        Assert.IsNotNull(fontEdge);
        Assert.AreEqual(GDResourceReferenceSource.SceneExtResource, fontEdge.Source);
    }

    [TestMethod]
    public void GraphBuild_ResourceNode_HasCorrectType()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        var texNode = graph.GetResource("res://icon.png");
        Assert.IsNotNull(texNode);
        Assert.AreEqual("Texture2D", texNode.ResourceType);
        Assert.AreEqual(GDResourceCategory.Texture, texNode.Category);
    }

    #endregion

    #region Category Resolver

    [TestMethod]
    public void CategoryResolver_PngExtension_ReturnsTexture()
    {
        Assert.AreEqual(GDResourceCategory.Texture, GDResourceCategoryResolver.CategoryFromExtension("res://icon.png"));
        Assert.AreEqual("Texture2D", GDResourceCategoryResolver.TypeNameFromExtension("res://icon.png"));
    }

    [TestMethod]
    public void CategoryResolver_OggExtension_ReturnsAudio()
    {
        Assert.AreEqual(GDResourceCategory.Audio, GDResourceCategoryResolver.CategoryFromExtension("res://click.ogg"));
        Assert.AreEqual("AudioStream", GDResourceCategoryResolver.TypeNameFromExtension("res://click.ogg"));
    }

    [TestMethod]
    public void CategoryResolver_TtfExtension_ReturnsFont()
    {
        Assert.AreEqual(GDResourceCategory.Font, GDResourceCategoryResolver.CategoryFromExtension("res://font.ttf"));
        Assert.AreEqual("Font", GDResourceCategoryResolver.TypeNameFromExtension("res://font.ttf"));
    }

    [TestMethod]
    public void CategoryResolver_TypeName_Texture2D_ReturnsTexture()
    {
        Assert.AreEqual(GDResourceCategory.Texture, GDResourceCategoryResolver.CategoryFromTypeName("Texture2D"));
    }

    [TestMethod]
    public void CategoryResolver_TypeName_AudioStream_ReturnsAudio()
    {
        Assert.AreEqual(GDResourceCategory.Audio, GDResourceCategoryResolver.CategoryFromTypeName("AudioStream"));
    }

    [TestMethod]
    public void CategoryResolver_PropertyName_ReturnsExpectedType()
    {
        Assert.AreEqual("Texture2D", GDResourceCategoryResolver.PropertyToResourceType("texture"));
        Assert.AreEqual("Material", GDResourceCategoryResolver.PropertyToResourceType("material"));
        Assert.AreEqual("AudioStream", GDResourceCategoryResolver.PropertyToResourceType("stream"));
        Assert.AreEqual("Theme", GDResourceCategoryResolver.PropertyToResourceType("theme"));
        Assert.AreEqual("Mesh", GDResourceCategoryResolver.PropertyToResourceType("mesh"));
        Assert.AreEqual("Font", GDResourceCategoryResolver.PropertyToResourceType("font"));
    }

    [TestMethod]
    public void CategoryResolver_ThemeOverridePath_ReturnsFont()
    {
        Assert.AreEqual("Font", GDResourceCategoryResolver.PropertyToResourceType("theme_override_fonts/font"));
        Assert.AreEqual("Texture2D", GDResourceCategoryResolver.PropertyToResourceType("theme_override_icons/icon"));
        Assert.AreEqual("StyleBox", GDResourceCategoryResolver.PropertyToResourceType("theme_override_styles/normal"));
    }

    #endregion

    #region TresParser

    [TestMethod]
    public void TresParser_SpriteFramesHeader_ReturnsSpriteFrames()
    {
        var content = @"[gd_resource type=""SpriteFrames"" load_steps=5 format=3]";
        Assert.AreEqual("SpriteFrames", GDTresParser.ParseResourceType(content));
    }

    [TestMethod]
    public void TresParser_ThemeHeader_ReturnsTheme()
    {
        var content = @"[gd_resource type=""Theme"" load_steps=2 format=3]";
        Assert.AreEqual("Theme", GDTresParser.ParseResourceType(content));
    }

    [TestMethod]
    public void TresParser_MaterialHeader_ReturnsShaderMaterial()
    {
        var content = @"[gd_resource type=""ShaderMaterial"" load_steps=2 format=3]";
        Assert.AreEqual("ShaderMaterial", GDTresParser.ParseResourceType(content));
    }

    [TestMethod]
    public void TresParser_NoHeader_ReturnsNull()
    {
        Assert.IsNull(GDTresParser.ParseResourceType(""));
        Assert.IsNull(GDTresParser.ParseResourceType("some random text"));
    }

    [TestMethod]
    public void TresParser_ExtResources_AreParsed()
    {
        var content = @"
[gd_resource type=""Theme"" load_steps=3 format=3]

[ext_resource type=""Font"" path=""res://font.ttf"" id=""1_font""]
[ext_resource type=""Texture2D"" path=""res://icon.png"" id=""2_tex""]
";

        var extRes = GDTresParser.ParseExtResources(content);
        Assert.AreEqual(2, extRes.Count);
        Assert.AreEqual("res://font.ttf", extRes[0].Path);
        Assert.AreEqual("Font", extRes[0].Type);
        Assert.AreEqual("res://icon.png", extRes[1].Path);
        Assert.AreEqual("Texture2D", extRes[1].Type);
    }

    #endregion

    #region Service API

    [TestMethod]
    public void Service_AnalyzeProject_ReturnsReport()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        // Verify the graph has expected content
        Assert.IsTrue(graph.ResourceCount >= 2);
        Assert.IsTrue(graph.EdgeCount >= 2);
    }

    [TestMethod]
    public void Service_ResourcesByCategory_FilterWorks()
    {
        var (graph, _) = BuildTestGraphWithProvider();

        var textures = graph.AllResources.Where(r => r.Category == GDResourceCategory.Texture).ToList();
        Assert.IsTrue(textures.Count >= 1);
        Assert.IsTrue(textures.Any(t => t.ResourcePath == "res://icon.png"));
    }

    [TestMethod]
    public void Service_GetResourceType_ResolvesFromScene()
    {
        var (_, provider) = BuildTestGraphWithProvider();

        var resourceType = provider.GetResourceType("res://icon.png");
        Assert.AreEqual("Texture2D", resourceType);
    }

    #endregion

    #region Helpers

    private (GDResourceFlowGraph, GDSceneTypesProvider) BuildTestGraphWithProvider()
    {
        var sceneContent = @"
[gd_scene load_steps=3 format=3]

[ext_resource type=""Texture2D"" path=""res://icon.png"" id=""1_tex""]
[ext_resource type=""Font"" path=""res://font.ttf"" id=""2_fnt""]

[node name=""Level"" type=""Node2D""]

[node name=""Sprite"" type=""Sprite2D"" parent="".""]
texture = ExtResource(""1_tex"")
";

        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "scenes", "level.tscn"), sceneContent);

        var provider = new GDSceneTypesProvider(projectPath, mockFs);
        provider.LoadScene("res://scenes/level.tscn");

        var builder = new GDResourceFlowBuilder(null!, provider);
        var graph = builder.Build();

        return (graph, provider);
    }

    #endregion
}
