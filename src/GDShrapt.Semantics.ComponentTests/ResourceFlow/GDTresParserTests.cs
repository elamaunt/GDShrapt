using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class GDTresParserTests
{
    [TestMethod]
    public void ParseScriptClass_FromHeader_ReturnsClassName()
    {
        var content = @"[gd_resource type=""Resource"" script_class=""DialogicCharacter"" load_steps=2 format=3]

[resource]
display_name = ""Preview""
";

        var result = GDTresParser.ParseScriptClass(content);

        result.Should().Be("DialogicCharacter");
    }

    [TestMethod]
    public void ParseScriptClass_NoScriptClass_ReturnsNull()
    {
        var content = @"[gd_resource type=""StyleBoxFlat"" format=3]

[resource]
bg_color = Color(0, 0, 0, 1)
";

        var result = GDTresParser.ParseScriptClass(content);

        result.Should().BeNull();
    }

    [TestMethod]
    public void ParseScriptClass_EmptyContent_ReturnsNull()
    {
        GDTresParser.ParseScriptClass("").Should().BeNull();
        GDTresParser.ParseScriptClass(null!).Should().BeNull();
    }

    [TestMethod]
    public void ParseScriptExtResourceId_ResolvesId()
    {
        var content = @"[gd_resource type=""Resource"" script_class=""BattlerStats"" load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://src/battler_stats.gd"" id=""1_abc""]

[resource]
script = ExtResource(""1_abc"")
base_attack = 10
";

        var result = GDTresParser.ParseScriptExtResourceId(content);

        result.Should().Be("1_abc");
    }

    [TestMethod]
    public void ParseScriptExtResourceId_NoScript_ReturnsNull()
    {
        var content = @"[gd_resource type=""StyleBoxFlat"" format=3]

[resource]
bg_color = Color(0, 0, 0, 1)
";

        var result = GDTresParser.ParseScriptExtResourceId(content);

        result.Should().BeNull();
    }

    [TestMethod]
    public void ParseResourceProperties_ExtractsPropertyNames()
    {
        var content = @"[gd_resource type=""Resource"" script_class=""BattlerStats"" format=3]

[resource]
script = ExtResource(""1_abc"")
base_attack = 10
base_defense = 5
display_name = ""Warrior""
";

        var result = GDTresParser.ParseResourceProperties(content);

        result.Should().HaveCount(3);
        result.Select(p => p.Name).Should().Contain("base_attack", "base_defense", "display_name");
        result.Should().NotContain(p => p.Name == "script");
    }

    [TestMethod]
    public void ParseResourceProperties_ExtractsStringValues()
    {
        var content = @"[gd_resource type=""Resource"" format=3]

[resource]
display_name = ""Preview""
description = ""A test character""
portraits = { ""default"": { ""image"": ""res://icon.png"" } }
";

        var result = GDTresParser.ParseResourceProperties(content);

        var displayName = result.First(p => p.Name == "display_name");
        displayName.StringValues.Should().Contain("Preview");

        var portraits = result.First(p => p.Name == "portraits");
        portraits.StringValues.Should().Contain("default");
        portraits.StringValues.Should().Contain("image");
        portraits.StringValues.Should().Contain("res://icon.png");
    }

    [TestMethod]
    public void ParseResourceProperties_HandlesNestedDictionaries()
    {
        var content = @"[gd_resource type=""Resource"" format=3]

[resource]
custom_data = { ""health"": 100, ""name"": ""Hero"", ""nested"": { ""key"": ""value"" } }
";

        var result = GDTresParser.ParseResourceProperties(content);

        result.Should().HaveCount(1);
        var prop = result[0];
        prop.Name.Should().Be("custom_data");
        prop.StringValues.Should().Contain("health");
        prop.StringValues.Should().Contain("name");
        prop.StringValues.Should().Contain("Hero");
        prop.StringValues.Should().Contain("key");
        prop.StringValues.Should().Contain("value");
    }

    [TestMethod]
    public void ParseResourceProperties_EmptyContent_ReturnsEmpty()
    {
        GDTresParser.ParseResourceProperties("").Should().BeEmpty();
        GDTresParser.ParseResourceProperties(null!).Should().BeEmpty();
    }

    [TestMethod]
    public void ParseResourceProperties_NoResourceSection_ReturnsEmpty()
    {
        var content = @"[gd_resource type=""StyleBoxFlat"" format=3]

[sub_resource type=""Gradient"" id=""grad1""]
colors = PackedColorArray(0, 0, 0, 1)
";

        var result = GDTresParser.ParseResourceProperties(content);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void ParseResourceProperties_StopsAtNextSection()
    {
        var content = @"[gd_resource type=""Theme"" load_steps=2 format=3]

[sub_resource type=""StyleBoxFlat"" id=""1""]
bg_color = Color(0, 0, 0, 1)

[resource]
default_font_size = 20
";

        var result = GDTresParser.ParseResourceProperties(content);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("default_font_size");
        // Should not include sub_resource properties
        result.Should().NotContain(p => p.Name == "bg_color");
    }

    [TestMethod]
    public void ParseFull_CombinesAllSections()
    {
        var content = @"[gd_resource type=""Resource"" script_class=""DialogicCharacter"" load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://dialogic_character.gd"" id=""1_script""]

[resource]
script = ExtResource(""1_script"")
display_name = ""Preview""
color = Color(1, 0, 0, 1)
portraits = { ""default"": {} }
";

        var result = GDTresParser.ParseFull(content);

        result.ResourceType.Should().Be("Resource");
        result.ScriptClass.Should().Be("DialogicCharacter");
        result.ScriptExtResourceId.Should().Be("1_script");
        result.ExtResources.Should().HaveCount(1);
        result.ExtResources[0].Path.Should().Be("res://dialogic_character.gd");
        result.ResourceProperties.Should().HaveCount(3);
        result.ResourceProperties.Select(p => p.Name).Should().Contain("display_name", "color", "portraits");
    }

    [TestMethod]
    public void ParseResourceType_ExistingBehavior_StillWorks()
    {
        var content = @"[gd_resource type=""StyleBoxFlat"" format=3 uid=""uid://abc123""]";

        GDTresParser.ParseResourceType(content).Should().Be("StyleBoxFlat");
    }

    [TestMethod]
    public void ParseExtResources_ExistingBehavior_StillWorks()
    {
        var content = @"[gd_resource type=""Resource"" load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://script.gd"" id=""1_abc""]
[ext_resource type=""Texture2D"" path=""res://icon.png"" id=""2_tex""]

[resource]
script = ExtResource(""1_abc"")
";

        var result = GDTresParser.ParseExtResources(content);

        result.Should().HaveCount(2);
        result[0].Path.Should().Be("res://script.gd");
        result[0].Id.Should().Be("1_abc");
        result[1].Path.Should().Be("res://icon.png");
        result[1].Id.Should().Be("2_tex");
    }
}
