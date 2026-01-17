using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Semantics;

[TestClass]
public class GDNamingUtilitiesTests
{
    #region ToSnakeCase Tests

    [TestMethod]
    [DataRow("PlayerHealth", "player_health")]
    [DataRow("player_health", "player_health")]
    [DataRow("camelCase", "camel_case")]
    [DataRow("HTTPClient", "http_client")]
    [DataRow("XMLParser", "xml_parser")]
    [DataRow("getHTTPResponse", "get_http_response")]
    [DataRow("ID", "id")]
    [DataRow("getID", "get_id")]
    [DataRow("myXMLFile", "my_xml_file")]
    [DataRow("ABC", "abc")]
    [DataRow("ABCdef", "ab_cdef")]
    [DataRow("already_snake_case", "already_snake_case")]
    public void ToSnakeCase_ConvertsCorrectly(string input, string expected)
    {
        var result = GDNamingUtilities.ToSnakeCase(input);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ToSnakeCase_EmptyString_ReturnsDefault()
    {
        GDNamingUtilities.ToSnakeCase("").Should().Be("value");
        GDNamingUtilities.ToSnakeCase(null!).Should().Be("value");
    }

    [TestMethod]
    public void ToSnakeCase_StartsWithNumber_ReturnsDefault()
    {
        GDNamingUtilities.ToSnakeCase("123abc").Should().Be("value");
    }

    #endregion

    #region ToPascalCase Tests

    [TestMethod]
    [DataRow("player_health", "PlayerHealth")]
    [DataRow("get_http_response", "GetHttpResponse")]
    [DataRow("id", "Id")]
    [DataRow("PlayerHealth", "PlayerHealth")]
    [DataRow("my_xml_file", "MyXmlFile")]
    public void ToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        var result = GDNamingUtilities.ToPascalCase(input);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ToPascalCase_EmptyString_ReturnsDefault()
    {
        GDNamingUtilities.ToPascalCase("").Should().Be("Value");
        GDNamingUtilities.ToPascalCase(null!).Should().Be("Value");
    }

    #endregion

    #region ToScreamingSnakeCase Tests

    [TestMethod]
    [DataRow("PlayerHealth", "PLAYER_HEALTH")]
    [DataRow("maxValue", "MAX_VALUE")]
    [DataRow("DefaultSpeed", "DEFAULT_SPEED")]
    public void ToScreamingSnakeCase_ConvertsCorrectly(string input, string expected)
    {
        var result = GDNamingUtilities.ToScreamingSnakeCase(input);
        result.Should().Be(expected);
    }

    #endregion

    #region ValidateIdentifier Tests

    [TestMethod]
    public void ValidateIdentifier_ValidNames_ReturnsTrue()
    {
        GDNamingUtilities.ValidateIdentifier("player", out _).Should().BeTrue();
        GDNamingUtilities.ValidateIdentifier("_private", out _).Should().BeTrue();
        GDNamingUtilities.ValidateIdentifier("my_var_123", out _).Should().BeTrue();
        GDNamingUtilities.ValidateIdentifier("PlayerHealth", out _).Should().BeTrue();
    }

    [TestMethod]
    public void ValidateIdentifier_EmptyName_ReturnsFalse()
    {
        GDNamingUtilities.ValidateIdentifier("", out var error).Should().BeFalse();
        error.Should().Contain("empty");
    }

    [TestMethod]
    public void ValidateIdentifier_StartsWithNumber_ReturnsFalse()
    {
        GDNamingUtilities.ValidateIdentifier("123abc", out var error).Should().BeFalse();
        error.Should().Contain("start");
    }

    [TestMethod]
    public void ValidateIdentifier_InvalidCharacter_ReturnsFalse()
    {
        GDNamingUtilities.ValidateIdentifier("my-var", out var error).Should().BeFalse();
        error.Should().Contain("-");

        GDNamingUtilities.ValidateIdentifier("my var", out error).Should().BeFalse();
        error.Should().Contain(" ");
    }

    #endregion

    #region IsReservedKeyword Tests

    [TestMethod]
    [DataRow("func", true)]
    [DataRow("class", true)]
    [DataRow("var", true)]
    [DataRow("if", true)]
    [DataRow("for", true)]
    [DataRow("while", true)]
    [DataRow("return", true)]
    [DataRow("true", true)]
    [DataRow("false", true)]
    [DataRow("null", true)]
    [DataRow("self", true)]
    [DataRow("await", true)]
    [DataRow("player", false)]
    [DataRow("my_function", false)]
    [DataRow("Player", false)]
    public void IsReservedKeyword_ChecksCorrectly(string name, bool expected)
    {
        GDNamingUtilities.IsReservedKeyword(name).Should().Be(expected);
    }

    #endregion

    #region IsBuiltInType Tests

    [TestMethod]
    [DataRow("String", true)]
    [DataRow("int", true)]
    [DataRow("float", true)]
    [DataRow("bool", true)]
    [DataRow("Array", true)]
    [DataRow("Dictionary", true)]
    [DataRow("Vector2", true)]
    [DataRow("Vector3", true)]
    [DataRow("Node", true)]
    [DataRow("Node2D", true)]
    [DataRow("Variant", true)]
    [DataRow("Player", false)]
    [DataRow("MyClass", false)]
    public void IsBuiltInType_ChecksCorrectly(string name, bool expected)
    {
        GDNamingUtilities.IsBuiltInType(name).Should().Be(expected);
    }

    #endregion

    #region NormalizeVariableName Tests

    [TestMethod]
    [DataRow("Player Health", "player_health")]
    [DataRow("PlayerHealth", "playerhealth")]
    [DataRow("my-var", "myvar")]
    [DataRow("spaces", "spaces")]
    public void NormalizeVariableName_NormalizesCorrectly(string input, string expected)
    {
        GDNamingUtilities.NormalizeVariableName(input).Should().Be(expected);
    }

    [TestMethod]
    public void NormalizeVariableName_EmptyOrInvalid_ReturnsDefault()
    {
        GDNamingUtilities.NormalizeVariableName("").Should().Be("new_variable");
        GDNamingUtilities.NormalizeVariableName("   ").Should().Be("new_variable");
        GDNamingUtilities.NormalizeVariableName("123").Should().Be("new_variable");
    }

    #endregion

    #region NormalizeConstantName Tests

    [TestMethod]
    [DataRow("maxValue", "MAX_VALUE")]
    [DataRow("DefaultSpeed", "DEFAULT_SPEED")]
    public void NormalizeConstantName_NormalizesCorrectly(string input, string expected)
    {
        GDNamingUtilities.NormalizeConstantName(input).Should().Be(expected);
    }

    [TestMethod]
    public void NormalizeConstantName_Empty_ReturnsDefault()
    {
        GDNamingUtilities.NormalizeConstantName("").Should().Be("NEW_CONSTANT");
    }

    #endregion

    #region SuggestVariableFromNodePath Tests

    [TestMethod]
    [DataRow("Player/Camera2D", "camera2_d")]
    [DataRow("UI/HealthBar", "health_bar")]
    [DataRow("@Player", "player")]
    [DataRow("Enemy", "enemy")]
    public void SuggestVariableFromNodePath_SuggestsCorrectly(string path, string expected)
    {
        GDNamingUtilities.SuggestVariableFromNodePath(path).Should().Be(expected);
    }

    [TestMethod]
    public void SuggestVariableFromNodePath_Empty_ReturnsDefault()
    {
        GDNamingUtilities.SuggestVariableFromNodePath("").Should().Be("node");
        GDNamingUtilities.SuggestVariableFromNodePath(null!).Should().Be("node");
    }

    #endregion

    #region GenerateUniqueName Tests

    [TestMethod]
    public void GenerateUniqueName_NoConflict_ReturnsOriginal()
    {
        var existing = new HashSet<string> { "other", "names" };
        GDNamingUtilities.GenerateUniqueName("player", existing).Should().Be("player");
    }

    [TestMethod]
    public void GenerateUniqueName_WithConflict_ReturnsSuffixed()
    {
        var existing = new HashSet<string> { "player", "other" };
        GDNamingUtilities.GenerateUniqueName("player", existing).Should().Be("player_1");
    }

    [TestMethod]
    public void GenerateUniqueName_MultipleConflicts_IncrementsSuffix()
    {
        var existing = new HashSet<string> { "player", "player_1", "player_2" };
        GDNamingUtilities.GenerateUniqueName("player", existing).Should().Be("player_3");
    }

    #endregion
}
