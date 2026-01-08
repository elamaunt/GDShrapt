using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace GDShrapt.Plugin.Tests;

/// <summary>
/// Tests for GDScript identifier name validation logic.
/// These tests validate the same rules used in NameInputDialog.
/// </summary>
[TestClass]
public class NameValidationTests
{
    private static readonly Regex ValidIdentifierRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private static readonly string[] ReservedKeywords =
    {
        "if", "elif", "else", "for", "while", "match", "break", "continue",
        "pass", "return", "class", "class_name", "extends", "is", "as",
        "self", "signal", "func", "static", "const", "enum", "var",
        "onready", "export", "setget", "tool", "yield", "assert", "preload",
        "await", "in", "not", "and", "or", "true", "false", "null",
        "PI", "TAU", "INF", "NAN", "super"
    };

    private (bool isValid, string? errorMessage) ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Name cannot be empty");

        name = name.Trim();

        if (!ValidIdentifierRegex.IsMatch(name))
        {
            if (char.IsDigit(name[0]))
                return (false, "Name cannot start with a digit");

            return (false, "Name contains invalid characters");
        }

        var lowerName = name.ToLowerInvariant();
        foreach (var keyword in ReservedKeywords)
        {
            if (lowerName == keyword.ToLowerInvariant())
                return (false, $"'{keyword}' is a reserved keyword");
        }

        if (name.Length > 100)
            return (false, "Name is too long (max 100 characters)");

        return (true, null);
    }

    #region Valid Names Tests

    [TestMethod]
    [DataRow("myVariable")]
    [DataRow("my_variable")]
    [DataRow("_privateVar")]
    [DataRow("MyClass")]
    [DataRow("CONSTANT_VALUE")]
    [DataRow("_")]
    [DataRow("a")]
    [DataRow("A")]
    [DataRow("var1")]
    [DataRow("node_2d")]
    [DataRow("__dunder__")]
    public void ValidateName_ValidIdentifiers_ReturnsTrue(string name)
    {
        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsTrue(isValid, $"'{name}' should be valid");
        Assert.IsNull(errorMessage);
    }

    #endregion

    #region Invalid Names Tests - Empty/Whitespace

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    public void ValidateName_EmptyOrWhitespace_ReturnsFalse(string? name)
    {
        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsFalse(isValid);
        Assert.AreEqual("Name cannot be empty", errorMessage);
    }

    #endregion

    #region Invalid Names Tests - Starts With Digit

    [TestMethod]
    [DataRow("1variable")]
    [DataRow("123")]
    [DataRow("0test")]
    [DataRow("9_value")]
    public void ValidateName_StartsWithDigit_ReturnsFalse(string name)
    {
        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsFalse(isValid);
        Assert.AreEqual("Name cannot start with a digit", errorMessage);
    }

    #endregion

    #region Invalid Names Tests - Invalid Characters

    [TestMethod]
    [DataRow("my-variable")]
    [DataRow("my.variable")]
    [DataRow("my variable")]
    [DataRow("my@var")]
    [DataRow("my#var")]
    [DataRow("my$var")]
    [DataRow("my%var")]
    [DataRow("var!")]
    [DataRow("(test)")]
    [DataRow("test+value")]
    public void ValidateName_InvalidCharacters_ReturnsFalse(string name)
    {
        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsFalse(isValid);
        Assert.AreEqual("Name contains invalid characters", errorMessage);
    }

    #endregion

    #region Reserved Keywords Tests

    [TestMethod]
    [DataRow("if")]
    [DataRow("else")]
    [DataRow("for")]
    [DataRow("while")]
    [DataRow("func")]
    [DataRow("var")]
    [DataRow("const")]
    [DataRow("class")]
    [DataRow("return")]
    [DataRow("true")]
    [DataRow("false")]
    [DataRow("null")]
    [DataRow("self")]
    [DataRow("await")]
    [DataRow("signal")]
    public void ValidateName_ReservedKeyword_ReturnsFalse(string name)
    {
        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsFalse(isValid);
        Assert.IsNotNull(errorMessage);
        Assert.IsTrue(errorMessage.Contains("is a reserved keyword"));
    }

    [TestMethod]
    [DataRow("IF")]
    [DataRow("ELSE")]
    [DataRow("Var")]
    [DataRow("TRUE")]
    [DataRow("False")]
    [DataRow("NULL")]
    public void ValidateName_ReservedKeywordCaseInsensitive_ReturnsFalse(string name)
    {
        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsFalse(isValid);
        Assert.IsNotNull(errorMessage);
        Assert.IsTrue(errorMessage.Contains("is a reserved keyword"));
    }

    #endregion

    #region Length Tests

    [TestMethod]
    public void ValidateName_MaxLength100_IsValid()
    {
        var name = new string('a', 100);

        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsTrue(isValid);
        Assert.IsNull(errorMessage);
    }

    [TestMethod]
    public void ValidateName_Over100Characters_ReturnsFalse()
    {
        var name = new string('a', 101);

        var (isValid, errorMessage) = ValidateName(name);

        Assert.IsFalse(isValid);
        Assert.AreEqual("Name is too long (max 100 characters)", errorMessage);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ValidateName_NameWithLeadingTrailingSpaces_IsTrimmed()
    {
        var (isValid, errorMessage) = ValidateName("  myVar  ");

        Assert.IsTrue(isValid);
        Assert.IsNull(errorMessage);
    }

    [TestMethod]
    public void ValidateName_PI_IsReserved()
    {
        var (isValid, errorMessage) = ValidateName("PI");

        Assert.IsFalse(isValid);
        Assert.IsTrue(errorMessage?.Contains("PI") ?? false);
    }

    [TestMethod]
    public void ValidateName_TAU_IsReserved()
    {
        var (isValid, errorMessage) = ValidateName("TAU");

        Assert.IsFalse(isValid);
        Assert.IsTrue(errorMessage?.Contains("TAU") ?? false);
    }

    [TestMethod]
    public void ValidateName_INF_IsReserved()
    {
        var (isValid, errorMessage) = ValidateName("INF");

        Assert.IsFalse(isValid);
        Assert.IsTrue(errorMessage?.Contains("INF") ?? false);
    }

    [TestMethod]
    public void ValidateName_NAN_IsReserved()
    {
        var (isValid, errorMessage) = ValidateName("NAN");

        Assert.IsFalse(isValid);
        Assert.IsTrue(errorMessage?.Contains("NAN") ?? false);
    }

    #endregion
}
