using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDDeadCodeExcludeTests
{
    [TestMethod]
    public void ShouldSkipFile_EmptyPatterns_NeverExcludes()
    {
        var options = new GDDeadCodeOptions();
        options.ShouldSkipFile("addons/plugin/script.gd").Should().BeFalse();
    }

    [TestMethod]
    public void ShouldSkipFile_WithAddonsPattern_SkipsAddonsFiles()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**" }
        };

        options.ShouldSkipFile("addons/plugin/script.gd").Should().BeTrue();
        options.ShouldSkipFile("addons/other/deep/file.gd").Should().BeTrue();
    }

    [TestMethod]
    public void ShouldSkipFile_WithAddonsPattern_DoesNotSkipOtherFiles()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**" }
        };

        options.ShouldSkipFile("scripts/player.gd").Should().BeFalse();
        options.ShouldSkipFile("scenes/main.gd").Should().BeFalse();
    }

    [TestMethod]
    public void ShouldSkipFile_WithDotGodotPattern_SkipsDotGodotFiles()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { ".godot/**" }
        };

        options.ShouldSkipFile(".godot/imported/resource.gd").Should().BeTrue();
    }

    [TestMethod]
    public void ShouldSkipFile_WithBackslashPaths_NormalizesAndMatches()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**" }
        };

        options.ShouldSkipFile("addons\\plugin\\script.gd").Should().BeTrue();
    }

    [TestMethod]
    public void ShouldSkipFile_MultiplePatterns_MatchesAny()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**", ".godot/**", "test/**" }
        };

        options.ShouldSkipFile("addons/plugin.gd").Should().BeTrue();
        options.ShouldSkipFile(".godot/file.gd").Should().BeTrue();
        options.ShouldSkipFile("test/test_player.gd").Should().BeTrue();
        options.ShouldSkipFile("scripts/player.gd").Should().BeFalse();
    }

    [TestMethod]
    public void ShouldSkipFile_CombinesExcludePatternsAndTestExclusion()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**" },
            ExcludeTestFiles = true
        };

        options.ShouldSkipFile("addons/plugin.gd").Should().BeTrue();
        options.ShouldSkipFile("test/test_player.gd").Should().BeTrue();
        options.ShouldSkipFile("scripts/player.gd").Should().BeFalse();
    }

    [TestMethod]
    public void ShouldSkipFile_ExcludePatternsOnly_DoesNotAffectTestExclusion()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**" },
            ExcludeTestFiles = false
        };

        options.ShouldSkipFile("test/test_player.gd").Should().BeFalse();
    }

    [TestMethod]
    public void WithStrictConfidenceOnly_CopiesExcludePatterns()
    {
        var options = new GDDeadCodeOptions
        {
            ExcludePatterns = new List<string> { "addons/**", ".godot/**" },
            ExcludeTestFiles = true
        };

        var strict = options.WithStrictConfidenceOnly();

        strict.ExcludePatterns.Should().BeEquivalentTo(options.ExcludePatterns);
        strict.ExcludeTestFiles.Should().Be(options.ExcludeTestFiles);
    }
}
