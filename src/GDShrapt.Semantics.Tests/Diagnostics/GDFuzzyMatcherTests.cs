using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDFuzzyMatcherTests
{
    #region LevenshteinDistance Tests

    [TestMethod]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        GDFuzzyMatcher.LevenshteinDistance("attack", "attack").Should().Be(0);
    }

    [TestMethod]
    public void LevenshteinDistance_EmptyStrings_ReturnsZero()
    {
        GDFuzzyMatcher.LevenshteinDistance("", "").Should().Be(0);
    }

    [TestMethod]
    public void LevenshteinDistance_OneEmptyString_ReturnsLengthOfOther()
    {
        GDFuzzyMatcher.LevenshteinDistance("attack", "").Should().Be(6);
        GDFuzzyMatcher.LevenshteinDistance("", "attack").Should().Be(6);
    }

    [TestMethod]
    public void LevenshteinDistance_SingleCharacterDifference_ReturnsOne()
    {
        // Substitution
        GDFuzzyMatcher.LevenshteinDistance("attack", "atteck").Should().Be(1);
        // Insertion
        GDFuzzyMatcher.LevenshteinDistance("attack", "atttack").Should().Be(1);
        // Deletion
        GDFuzzyMatcher.LevenshteinDistance("attack", "attck").Should().Be(1);
    }

    [TestMethod]
    public void LevenshteinDistance_CommonTypos_ReturnsExpectedDistance()
    {
        // "atack" instead of "attack" (missing 't')
        GDFuzzyMatcher.LevenshteinDistance("attack", "atack").Should().Be(1);

        // "positon" instead of "position" (missing 'i')
        GDFuzzyMatcher.LevenshteinDistance("position", "positon").Should().Be(1);

        // "heatlh" instead of "health" (transposition)
        GDFuzzyMatcher.LevenshteinDistance("health", "heatlh").Should().Be(2);
    }

    [TestMethod]
    public void LevenshteinDistance_CompletelyDifferent_ReturnsHighValue()
    {
        GDFuzzyMatcher.LevenshteinDistance("attack", "defend").Should().BeGreaterThan(3);
    }

    #endregion

    #region AreSimilar Tests

    [TestMethod]
    public void AreSimilar_IdenticalStrings_ReturnsFalse()
    {
        // Distance is 0, which is not > 0
        GDFuzzyMatcher.AreSimilar("attack", "attack").Should().BeFalse();
    }

    [TestMethod]
    public void AreSimilar_SingleTypo_ReturnsTrue()
    {
        GDFuzzyMatcher.AreSimilar("attack", "atack").Should().BeTrue();
        GDFuzzyMatcher.AreSimilar("health", "heatlh").Should().BeTrue();
    }

    [TestMethod]
    public void AreSimilar_TooManyDifferences_ReturnsFalse()
    {
        GDFuzzyMatcher.AreSimilar("attack", "defend").Should().BeFalse();
    }

    [TestMethod]
    public void AreSimilar_CaseInsensitive_ReturnsTrue()
    {
        GDFuzzyMatcher.AreSimilar("Attack", "atack").Should().BeTrue();
        GDFuzzyMatcher.AreSimilar("HEALTH", "heatlh").Should().BeTrue();
    }

    [TestMethod]
    public void AreSimilar_NullOrEmpty_ReturnsFalse()
    {
        GDFuzzyMatcher.AreSimilar("", "attack").Should().BeFalse();
        GDFuzzyMatcher.AreSimilar("attack", "").Should().BeFalse();
        GDFuzzyMatcher.AreSimilar(null!, "attack").Should().BeFalse();
        GDFuzzyMatcher.AreSimilar("attack", null!).Should().BeFalse();
    }

    #endregion

    #region FindSimilar Tests

    [TestMethod]
    public void FindSimilar_FindsTypoInCandidates()
    {
        var candidates = new[] { "attack", "defend", "position", "health" };

        var results = GDFuzzyMatcher.FindSimilar("atack", candidates).ToList();

        results.Should().Contain("attack");
    }

    [TestMethod]
    public void FindSimilar_OrdersByDistance()
    {
        var candidates = new[] { "attack", "attacks", "attacker" };

        var results = GDFuzzyMatcher.FindSimilar("atack", candidates).ToList();

        // "attack" should come first (distance 1), then "attacks" (distance 2)
        results.Should().HaveCountGreaterThan(0);
        results[0].Should().Be("attack");
    }

    [TestMethod]
    public void FindSimilar_RespectsMaxResults()
    {
        var candidates = new[] { "attack", "attacks", "attacker", "attacking" };

        var results = GDFuzzyMatcher.FindSimilar("atack", candidates, maxResults: 2).ToList();

        results.Should().HaveCountLessThanOrEqualTo(2);
    }

    [TestMethod]
    public void FindSimilar_ExcludesExactMatch()
    {
        var candidates = new[] { "attack", "defend" };

        var results = GDFuzzyMatcher.FindSimilar("attack", candidates).ToList();

        // "attack" has distance 0, so should not be included
        results.Should().NotContain("attack");
    }

    [TestMethod]
    public void FindSimilar_ExcludesTooDifferent()
    {
        var candidates = new[] { "completely_different_name", "another_method" };

        var results = GDFuzzyMatcher.FindSimilar("attack", candidates).ToList();

        results.Should().BeEmpty();
    }

    [TestMethod]
    public void FindSimilar_EmptyName_ReturnsEmpty()
    {
        var candidates = new[] { "attack", "defend" };

        var results = GDFuzzyMatcher.FindSimilar("", candidates).ToList();

        results.Should().BeEmpty();
    }

    [TestMethod]
    public void FindSimilar_EmptyCandidates_ReturnsEmpty()
    {
        var results = GDFuzzyMatcher.FindSimilar("attack", Array.Empty<string>()).ToList();

        results.Should().BeEmpty();
    }

    [TestMethod]
    public void FindSimilar_GodotMethodTypos()
    {
        var godotMethods = new[]
        {
            "get_node", "get_parent", "add_child", "remove_child",
            "set_position", "get_position", "queue_free", "is_inside_tree"
        };

        // Common typos
        GDFuzzyMatcher.FindSimilar("get_nod", godotMethods).Should().Contain("get_node");
        GDFuzzyMatcher.FindSimilar("add_chld", godotMethods).Should().Contain("add_child");
        GDFuzzyMatcher.FindSimilar("quee_free", godotMethods).Should().Contain("queue_free");
    }

    [TestMethod]
    public void FindSimilar_PropertyTypos()
    {
        var properties = new[]
        {
            "position", "rotation", "scale", "visible", "modulate", "global_position"
        };

        GDFuzzyMatcher.FindSimilar("positon", properties).Should().Contain("position");
        GDFuzzyMatcher.FindSimilar("rotatoin", properties).Should().Contain("rotation");
        GDFuzzyMatcher.FindSimilar("visble", properties).Should().Contain("visible");
    }

    #endregion
}
