using FluentAssertions;
using GDShrapt.Reader.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for load/preload resource path validation.
    /// </summary>
    [TestClass]
    public class LoadPreloadValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        #region preload Tests

        [TestMethod]
        public void Preload_StaticPath_WithMockProvider_ResourceExists_NoWarning()
        {
            var code = @"
extends Node

var scene = preload(""res://scenes/main.tscn"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://scenes/main.tscn");

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Preload_StaticPath_ResourceNotExists_ReportsWarning()
        {
            var code = @"
extends Node

var scene = preload(""res://nonexistent.tscn"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            // Don't add resource - it doesn't exist

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Preload_StaticPath_ScriptExists_NoWarning()
        {
            var code = @"
extends Node

var script = preload(""res://scripts/player.gd"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddScript("res://scripts/player.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://scripts/player.gd",
                ClassName = "Player",
                BaseType = "Node2D"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Preload_WithoutProjectProvider_NoValidation()
        {
            var code = @"
extends Node

var scene = preload(""res://nonexistent.tscn"")
";
            // Use default provider (not IGDProjectRuntimeProvider)
            var options = GDValidationOptions.Default;

            var result = _validator.ValidateCode(code, options);

            // Without project provider, no resource validation should occur
            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Preload_CheckResourcePathsDisabled_NoValidation()
        {
            var code = @"
extends Node

var scene = preload(""res://nonexistent.tscn"")
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = false  // Disabled
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Preload_MultipleResources_MixedExistence()
        {
            var code = @"
extends Node

var scene1 = preload(""res://exists.tscn"")
var scene2 = preload(""res://not_exists.tscn"")
var scene3 = preload(""res://also_exists.gd"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://exists.tscn");
            mockProvider.AddScript("res://also_exists.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://also_exists.gd"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            var notFoundWarnings = result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .ToList();

            // Only one resource doesn't exist
            notFoundWarnings.Should().HaveCount(1);
            notFoundWarnings[0].Message.Should().Contain("res://not_exists.tscn");
        }

        #endregion

        #region load Tests

        [TestMethod]
        public void Load_StaticPath_ResourceNotExists_ReportsWarning()
        {
            var code = @"
extends Node

func test():
    var res = load(""res://missing.gd"")
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Load_StaticPath_ResourceExists_NoWarning()
        {
            var code = @"
extends Node

func test():
    var res = load(""res://data/config.tres"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://data/config.tres");

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Load_DynamicPath_NoValidation()
        {
            var code = @"
extends Node

func test():
    var path = ""res://dynamic.gd""
    var res = load(path)
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            // Dynamic paths cannot be validated
            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Load_ConcatenatedPath_NoValidation()
        {
            var code = @"
extends Node

func test():
    var res = load(""res://data/"" + ""file.tres"")
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            // Concatenated paths cannot be validated at compile time
            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Load_EmptyPath_ReportsResourceNotFound()
        {
            var code = @"
extends Node

func test():
    var res = load("""")
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            // Empty path is considered a resource that doesn't exist
            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Various Resource Types Tests

        [TestMethod]
        public void Preload_TextureResource_Exists_NoWarning()
        {
            var code = @"
extends Node

var tex = preload(""res://sprites/icon.png"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://sprites/icon.png");

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Preload_AudioResource_NotExists_ReportsWarning()
        {
            var code = @"
extends Node

var sound = preload(""res://audio/missing.ogg"")
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Load_InsideFunction_MultipleLoads()
        {
            var code = @"
extends Node

func load_resources():
    var a = load(""res://exists1.tres"")
    var b = load(""res://missing1.tres"")
    var c = preload(""res://exists2.tscn"")
    var d = preload(""res://missing2.tscn"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://exists1.tres");
            mockProvider.AddResource("res://exists2.tscn");

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckResourcePaths = true
            };

            var result = _validator.ValidateCode(code, options);

            var notFoundWarnings = result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ResourceNotFound)
                .ToList();

            notFoundWarnings.Should().HaveCount(2);
            notFoundWarnings.Should().Contain(w => w.Message.Contains("res://missing1.tres"));
            notFoundWarnings.Should().Contain(w => w.Message.Contains("res://missing2.tscn"));
        }

        #endregion
    }
}
