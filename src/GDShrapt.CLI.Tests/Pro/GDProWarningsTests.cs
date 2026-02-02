using System.IO;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests.Pro;

[TestClass]
public class GDProWarningsTests
{
    [TestMethod]
    [DataRow(GDProFeatureKind.ConfidenceAwarePotential, "Potential confidence mode")]
    [DataRow(GDProFeatureKind.ConfidenceAwareNameMatch, "NameMatch confidence mode")]
    [DataRow(GDProFeatureKind.BatchRefactoring, "Batch refactoring")]
    [DataRow(GDProFeatureKind.ApplyTypeAnnotations, "Apply type annotations")]
    [DataRow(GDProFeatureKind.BatchTypeAnnotations, "Batch add type annotations")]
    [DataRow(GDProFeatureKind.GenerateOnready, "Generate @onready")]
    [DataRow(GDProFeatureKind.ApplyReorderMembers, "Apply member reordering")]
    [DataRow(GDProFeatureKind.BatchReorderMembers, "Batch reorder members")]
    [DataRow(GDProFeatureKind.PersistentCache, "Persistent cache")]
    [DataRow(GDProFeatureKind.SarifExport, "SARIF export")]
    [DataRow(GDProFeatureKind.BaselineComparison, "Baseline comparison")]
    [DataRow(GDProFeatureKind.CIAnnotations, "CI annotations")]
    [DataRow(GDProFeatureKind.PolicyEnforcement, "Policy enforcement")]
    [DataRow(GDProFeatureKind.HtmlReports, "HTML reports")]
    [DataRow(GDProFeatureKind.DebtScoring, "Technical debt scoring")]
    [DataRow(GDProFeatureKind.DeadCodeRemoval, "Dead code removal")]
    [DataRow(GDProFeatureKind.DependencyVisualization, "Dependency visualization")]
    [DataRow(GDProFeatureKind.MetricsGates, "Metrics gates")]
    [DataRow(GDProFeatureKind.CoverageGates, "Coverage gates")]
    [DataRow(GDProFeatureKind.PatchExport, "Patch export")]
    [DataRow(GDProFeatureKind.SafeDeletionPlan, "Safe deletion plan")]
    [DataRow(GDProFeatureKind.DuplicateDetection, "Duplicate detection")]
    [DataRow(GDProFeatureKind.SecurityScanning, "Security scanning")]
    [DataRow(GDProFeatureKind.TrendAnalysis, "Trend analysis")]
    [DataRow(GDProFeatureKind.SafeInline, "Safe inline")]
    [DataRow(GDProFeatureKind.DuplicateExtractMethod, "Duplicate extract method")]
    public void GetFeatureName_ReturnsExpectedName(GDProFeatureKind feature, string expectedName)
    {
        // Act
        var result = GDProWarnings.GetFeatureName(feature);

        // Assert
        result.Should().Be(expectedName);
    }

    [TestMethod]
    public void WriteProNotAvailable_WhenWasEverLicensed_ContainsExpectedInfo()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteProNotAvailable(GDProFeatureKind.SarifExport, output, wasEverLicensed: true);

        // Assert
        var text = output.ToString();
        text.Should().Contain("[Warning]");
        text.Should().Contain("SARIF export");
        text.Should().Contain("not available in this build");
        text.Should().Contain("https://gdshrapt.com/pro");
    }

    [TestMethod]
    public void WriteProNotAvailable_WhenNeverLicensed_WritesNothing()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteProNotAvailable(GDProFeatureKind.SarifExport, output, wasEverLicensed: false);

        // Assert
        output.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void WriteLicenseRequired_WhenWasEverLicensed_ContainsRenewalInstructions()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteLicenseRequired(
            GDProFeatureKind.BatchRefactoring,
            GDProLicenseState.Expired,
            output,
            wasEverLicensed: true);

        // Assert
        var text = output.ToString();
        text.Should().Contain("[Warning]");
        text.Should().Contain("License expired");
        text.Should().Contain("Batch refactoring");
        text.Should().Contain("License status:");
        text.Should().Contain("Expired");
        text.Should().Contain("gdshrapt license activate");
        text.Should().Contain("https://gdshrapt.com/pro");
    }

    [TestMethod]
    public void WriteLicenseRequired_WhenNeverLicensed_WritesNothing()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteLicenseRequired(
            GDProFeatureKind.BatchRefactoring,
            GDProLicenseState.NotFound,
            output,
            wasEverLicensed: false);

        // Assert
        output.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void WriteLicenseRequired_WithExpiredState_ShowsExpired()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteLicenseRequired(
            GDProFeatureKind.DeadCodeRemoval,
            GDProLicenseState.Expired,
            output,
            wasEverLicensed: true);

        // Assert
        var text = output.ToString();
        text.Should().Contain("Expired");
        text.Should().Contain("Dead code removal");
    }

    [TestMethod]
    public void WriteLicenseRequired_WithInvalidState_ShowsInvalid()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteLicenseRequired(
            GDProFeatureKind.HtmlReports,
            GDProLicenseState.Invalid,
            output,
            wasEverLicensed: true);

        // Assert
        var text = output.ToString();
        text.Should().Contain("Invalid signature");
    }

    [TestMethod]
    public void WriteFeatureNotEnabled_ContainsExpectedInfo()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteFeatureNotEnabled(GDProFeatureKind.DebtScoring, output);

        // Assert
        var text = output.ToString();
        text.Should().Contain("[Warning]");
        text.Should().Contain("Feature not enabled");
        text.Should().Contain("Technical debt scoring");
        text.Should().Contain("license may not include");
        text.Should().Contain("https://gdshrapt.com/support");
    }

    [TestMethod]
    [DataRow(GDProLicenseState.ProNotAvailable, "Pro module not available")]
    [DataRow(GDProLicenseState.Valid, "Valid")]
    [DataRow(GDProLicenseState.Expired, "Expired")]
    [DataRow(GDProLicenseState.Invalid, "Invalid signature")]
    [DataRow(GDProLicenseState.Trial, "Trial")]
    [DataRow(GDProLicenseState.NotFound, "Not found")]
    [DataRow(GDProLicenseState.Perpetual, "Perpetual (valid)")]
    [DataRow(GDProLicenseState.UpdatesExpired, "Perpetual (updates expired for this version)")]
    public void GetLicenseStateMessage_ReturnsExpectedMessage(GDProLicenseState state, string expected)
    {
        // Act
        var result = GDProWarnings.GetLicenseStateMessage(state);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    public void GetShortWarning_WhenWasEverLicensed_ContainsFeatureName()
    {
        // Act
        var result = GDProWarnings.GetShortWarning(GDProFeatureKind.CIAnnotations, wasEverLicensed: true);

        // Assert
        result.Should().Be("[License expired: CI annotations]");
    }

    [TestMethod]
    public void GetShortWarning_WhenNeverLicensed_ReturnsEmpty()
    {
        // Act
        var result = GDProWarnings.GetShortWarning(GDProFeatureKind.CIAnnotations, wasEverLicensed: false);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetShortWarning_AllFeatures_WhenWasEverLicensed_ReturnsValidFormat()
    {
        // Test all features
        var features = Enum.GetValues<GDProFeatureKind>();

        foreach (var feature in features)
        {
            // Act
            var result = GDProWarnings.GetShortWarning(feature, wasEverLicensed: true);

            // Assert
            result.Should().StartWith("[License expired: ");
            result.Should().EndWith("]");
            result.Should().NotContain("GDProFeatureKind"); // Should not contain enum type name
        }
    }
}
