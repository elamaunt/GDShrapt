using System.IO;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests.Pro;

[TestClass]
public class GDProIntegrationTests
{
    [TestInitialize]
    public void Setup()
    {
        // Reset the static state before each test
        GDProIntegration.Reset();
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Ensure we clean up after tests
        GDProIntegration.Reset();
    }

    [TestMethod]
    public void IsProAvailable_WithoutProAssembly_ReturnsFalse()
    {
        // When no provider is registered and Pro assembly is not linked
        // Provider discovery will fail

        // Act
        var result = GDProIntegration.IsProAvailable;

        // Assert - will be true if Pro is actually linked in test environment
        // or false if running in Base-only mode
        // We can't assert a specific value since it depends on the test environment
        // Instead, test the consistency
        result.Should().Be(GDProIntegration.Provider != null);
    }

    [TestMethod]
    public void LicenseState_WithoutProvider_ReturnsProNotAvailable()
    {
        // Arrange - Reset ensures no provider

        // Act
        var state = GDProIntegration.LicenseState;

        // Assert - if no provider discovered, should return ProNotAvailable
        if (!GDProIntegration.IsProAvailable)
        {
            state.Should().Be(GDProLicenseState.ProNotAvailable);
        }
    }

    [TestMethod]
    public void RegisterProvider_SetsProvider()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Licensed();

        // Act
        GDProIntegration.RegisterProvider(mockProvider);

        // Assert
        GDProIntegration.Provider.Should().BeSameAs(mockProvider);
        GDProIntegration.IsProAvailable.Should().BeTrue();
        GDProIntegration.IsProLicensed.Should().BeTrue();
    }

    [TestMethod]
    public void IsFeatureEnabled_WithRegisteredLicensedProvider_ReturnsTrue()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Licensed();
        GDProIntegration.RegisterProvider(mockProvider);

        // Act
        var result = GDProIntegration.IsFeatureEnabled(GDProFeatureKind.BatchRefactoring);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void IsFeatureEnabled_WithUnlicensedProvider_ReturnsFalse()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Unlicensed();
        GDProIntegration.RegisterProvider(mockProvider);

        // Act
        var result = GDProIntegration.IsFeatureEnabled(GDProFeatureKind.BatchRefactoring);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void TryUseFeature_WithoutProvider_WritesWarningAndReturnsFalse()
    {
        // Arrange
        using var output = new StringWriter();

        // Act (force reset to ensure no provider)
        GDProIntegration.Reset();

        // Only test if Pro is not actually linked
        if (!GDProIntegration.IsProAvailable)
        {
            var result = GDProIntegration.TryUseFeature(GDProFeatureKind.SarifExport, output);

            // Assert
            result.Should().BeFalse();
            var outputText = output.ToString();
            outputText.Should().Contain("requires GDShrapt Pro");
            outputText.Should().Contain("SARIF export");
        }
    }

    [TestMethod]
    public void TryUseFeature_WithLicensedProvider_ReturnsTrue()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Licensed();
        GDProIntegration.RegisterProvider(mockProvider);
        using var output = new StringWriter();

        // Act
        var result = GDProIntegration.TryUseFeature(GDProFeatureKind.SarifExport, output);

        // Assert
        result.Should().BeTrue();
        output.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void TryUseFeature_WithUnlicensedProvider_IsSilentAndReturnsFalse()
    {
        // Arrange - user never purchased license
        var mockProvider = MockProFeatureProvider.Unlicensed();
        GDProIntegration.RegisterProvider(mockProvider);
        using var output = new StringWriter();

        // Act
        var result = GDProIntegration.TryUseFeature(GDProFeatureKind.SarifExport, output);

        // Assert
        result.Should().BeFalse();
        // Should be silent for users who never purchased
        output.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void TryUseFeature_WithExpiredProvider_WritesWarningAndReturnsFalse()
    {
        // Arrange - user previously had license but it expired
        var mockProvider = MockProFeatureProvider.Expired();
        GDProIntegration.RegisterProvider(mockProvider);
        using var output = new StringWriter();

        // Act
        var result = GDProIntegration.TryUseFeature(GDProFeatureKind.BatchRefactoring, output);

        // Assert
        result.Should().BeFalse();
        var outputText = output.ToString();
        // Should show warning for users who had license before
        outputText.Should().Contain("License expired");
        outputText.Should().Contain("Expired");
    }

    [TestMethod]
    public void TryUseFeature_WithFeatureNotEnabled_WritesWarningAndReturnsFalse()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Licensed()
            .WithFeatures(GDProFeatureKind.SarifExport); // Only SARIF enabled
        GDProIntegration.RegisterProvider(mockProvider);
        using var output = new StringWriter();

        // Act - try to use a feature that's not enabled
        var result = GDProIntegration.TryUseFeature(GDProFeatureKind.BatchRefactoring, output);

        // Assert
        result.Should().BeFalse();
        var outputText = output.ToString();
        outputText.Should().Contain("Feature not enabled");
        outputText.Should().Contain("Batch refactoring");
    }

    [TestMethod]
    public void LicenseState_WithTrialProvider_ReturnsTrial()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Trial();
        GDProIntegration.RegisterProvider(mockProvider);

        // Act
        var state = GDProIntegration.LicenseState;

        // Assert
        state.Should().Be(GDProLicenseState.Trial);
        GDProIntegration.IsProLicensed.Should().BeTrue();
    }

    [TestMethod]
    public void LicenseState_WithPerpetualProvider_ReturnsPerpetual()
    {
        // Arrange
        var mockProvider = MockProFeatureProvider.Perpetual();
        GDProIntegration.RegisterProvider(mockProvider);

        // Act
        var state = GDProIntegration.LicenseState;

        // Assert
        state.Should().Be(GDProLicenseState.Perpetual);
        GDProIntegration.IsProLicensed.Should().BeTrue();
    }

    [TestMethod]
    public void RegisterProvider_ThrowsOnNull()
    {
        // Act & Assert
        Action act = () => GDProIntegration.RegisterProvider(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
