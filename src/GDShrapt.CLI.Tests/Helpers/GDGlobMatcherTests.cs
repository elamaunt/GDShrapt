using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDGlobMatcherTests
{
    [TestMethod]
    public void Matches_DoubleStarOnly_MatchesEverything()
    {
        GDGlobMatcher.Matches("any/path/file.gd", "**").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_PrefixDoubleStar_MatchesAnyDirectory()
    {
        GDGlobMatcher.Matches("addons/plugin/script.gd", "addons/**").Should().BeTrue();
        GDGlobMatcher.Matches("addons/nested/deep/script.gd", "addons/**").Should().BeTrue();
        GDGlobMatcher.Matches("addons", "addons/**").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_PrefixDoubleStar_DoesNotMatchOtherDirectories()
    {
        GDGlobMatcher.Matches("src/addons/script.gd", "addons/**").Should().BeFalse();
        GDGlobMatcher.Matches("scripts/player.gd", "addons/**").Should().BeFalse();
    }

    [TestMethod]
    public void Matches_SuffixDoubleStar_MatchesAnySuffix()
    {
        GDGlobMatcher.Matches("test/test_player.gd", "**/test_*.gd").Should().BeTrue();
        GDGlobMatcher.Matches("test_player.gd", "**/test_*.gd").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_DotGodotPattern_MatchesDotGodotDirectory()
    {
        GDGlobMatcher.Matches(".godot/imported/file.gd", ".godot/**").Should().BeTrue();
        GDGlobMatcher.Matches(".godot/editor/settings.gd", ".godot/**").Should().BeTrue();
        GDGlobMatcher.Matches(".godot", ".godot/**").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_DotGodotPattern_DoesNotMatchOther()
    {
        GDGlobMatcher.Matches("scripts/player.gd", ".godot/**").Should().BeFalse();
        GDGlobMatcher.Matches("godot/file.gd", ".godot/**").Should().BeFalse();
    }

    [TestMethod]
    public void Matches_SingleStar_MatchesWithinSegment()
    {
        GDGlobMatcher.Matches("test_player.gd", "test_*.gd").Should().BeTrue();
        GDGlobMatcher.Matches("script.gd", "*.gd").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_SingleStar_DoesNotMatchAcrossSegments()
    {
        GDGlobMatcher.Matches("dir/script.gd", "*.gd").Should().BeFalse();
    }

    [TestMethod]
    public void Matches_QuestionMark_MatchesSingleCharacter()
    {
        GDGlobMatcher.Matches("file1.gd", "file?.gd").Should().BeTrue();
        GDGlobMatcher.Matches("fileAB.gd", "file?.gd").Should().BeFalse();
    }

    [TestMethod]
    public void Matches_ExactMatch_MatchesExactPath()
    {
        GDGlobMatcher.Matches("addons", "addons").Should().BeTrue();
        GDGlobMatcher.Matches("addons/script.gd", "addons").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_ExactMatch_DoesNotMatchPartial()
    {
        GDGlobMatcher.Matches("addon", "addons").Should().BeFalse();
    }

    [TestMethod]
    public void Matches_MiddleDoubleStar_MatchesPrefixAndSuffix()
    {
        GDGlobMatcher.Matches("src/scripts/player.gd", "src/**/*.gd").Should().BeTrue();
        // ** matches zero or more segments, so src/player.gd also matches
        GDGlobMatcher.Matches("src/player.gd", "src/**/*.gd").Should().BeTrue();
        GDGlobMatcher.Matches("other/player.gd", "src/**/*.gd").Should().BeFalse();
    }

    [TestMethod]
    public void Matches_EmptyPath_DoesNotMatch()
    {
        GDGlobMatcher.Matches("", "addons/**").Should().BeFalse();
    }
}
