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
    [DataRow(GDProFeatureKind.IncrementalAnalysis, "Incremental analysis")]
    [DataRow(GDProFeatureKind.PersistentCache, "Persistent cache")]
    [DataRow(GDProFeatureKind.SarifExport, "SARIF export")]
    [DataRow(GDProFeatureKind.BaselineComparison, "Baseline comparison")]
    [DataRow(GDProFeatureKind.CIAnnotations, "CI annotations")]
    [DataRow(GDProFeatureKind.PolicyEnforcement, "Policy enforcement")]
    [DataRow(GDProFeatureKind.HtmlReports, "HTML reports")]
    [DataRow(GDProFeatureKind.DebtScoring, "Technical debt scoring")]
    public void GetFeatureName_ReturnsExpectedName(GDProFeatureKind feature, string expectedName)
    {
        // Act
        var result = GDProWarnings.GetFeatureName(feature);

        // Assert
        result.Should().Be(expectedName);
    }

    [TestMethod]
    public void WriteProNotAvailable_ContainsExpectedInfo()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteProNotAvailable(GDProFeatureKind.SarifExport, output);

        // Assert
        var text = output.ToString();
        text.Should().Contain("[Warning]");
        text.Should().Contain("SARIF export");
        text.Should().Contain("requires GDShrapt Pro");
        text.Should().Contain("This build does not include Pro features");
        text.Should().Contain("https://gdshrapt.com/pro");
    }

    [TestMethod]
    public void WriteLicenseRequired_ContainsActivationInstructions()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteLicenseRequired(
            GDProFeatureKind.BatchRefactoring,
            GDProLicenseState.NotFound,
            output);

        // Assert
        var text = output.ToString();
        text.Should().Contain("[Warning]");
        text.Should().Contain("Pro license required");
        text.Should().Contain("Batch refactoring");
        text.Should().Contain("License status:");
        text.Should().Contain("Not found");
        text.Should().Contain("gdshrapt license activate");
        text.Should().Contain("GDSHRAPT_LICENSE_PATH");
        text.Should().Contain("https://gdshrapt.com/pro");
    }

    [TestMethod]
    public void WriteLicenseRequired_WithExpiredState_ShowsExpired()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        GDProWarnings.WriteLicenseRequired(
            GDProFeatureKind.IncrementalAnalysis,
            GDProLicenseState.Expired,
            output);

        // Assert
        var text = output.ToString();
        text.Should().Contain("Expired");
        text.Should().Contain("Incremental analysis");
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
            output);

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
    public void GetShortWarning_ContainsFeatureName()
    {
        // Act
        var result = GDProWarnings.GetShortWarning(GDProFeatureKind.CIAnnotations);

        // Assert
        result.Should().Be("[Pro required: CI annotations]");
    }

    [TestMethod]
    public void GetShortWarning_AllFeatures_ReturnsValidFormat()
    {
        // Test all features
        var features = Enum.GetValues<GDProFeatureKind>();

        foreach (var feature in features)
        {
            // Act
            var result = GDProWarnings.GetShortWarning(feature);

            // Assert
            result.Should().StartWith("[Pro required: ");
            result.Should().EndWith("]");
            result.Should().NotContain("GDProFeatureKind"); // Should not contain enum type name
        }
    }
}
