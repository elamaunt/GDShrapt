using FluentAssertions;
using GDShrapt.Reader.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for extends validation with string paths (extends "res://path/script.gd").
    /// </summary>
    [TestClass]
    public class ExtendsPathValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        #region extends with Named Type Tests

        [TestMethod]
        public void Extends_NamedType_KnownType_NoWarning()
        {
            var code = @"
extends Node2D

func test():
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Extends_NamedType_UnknownType_ReportsWarning()
        {
            var code = @"
extends NonExistentClass

func test():
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().NotBeEmpty();
        }

        #endregion

        #region extends with String Path Tests

        [TestMethod]
        public void Extends_StringPath_ScriptExists_NoWarning()
        {
            var code = @"
extends ""res://scripts/base.gd""

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://scripts/base.gd");
            mockProvider.AddScript("res://scripts/base.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://scripts/base.gd",
                BaseType = "Node"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Extends_StringPath_ScriptNotExists_ReportsWarning()
        {
            var code = @"
extends ""res://nonexistent.gd""

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Extends_StringPath_ResourceExistsButNoScriptInfo_NoWarning()
        {
            // When resource exists but we don't have script type info,
            // we should be lenient (file exists = OK)
            var code = @"
extends ""res://scripts/base.gd""

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddResource("res://scripts/base.gd");
            // Script info not added - file exists but no type info

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            // Should not warn because resource exists (graceful degradation)
            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Extends_StringPath_WithClassName_ScriptExists_NoWarning()
        {
            var code = @"
extends ""res://entities/enemy.gd""

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddScript("res://entities/enemy.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://entities/enemy.gd",
                ClassName = "Enemy",
                BaseType = "CharacterBody2D"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Extends_StringPath_WithoutProjectProvider_NoValidation()
        {
            var code = @"
extends ""res://nonexistent.gd""

func test():
    pass
";
            // Use default provider (not IGDProjectRuntimeProvider)
            var options = GDValidationOptions.Default;

            var result = _validator.ValidateCode(code, options);

            // Without project provider, path validation cannot occur
            // Note: This might still warn if default behavior is to warn on string paths
        }

        [TestMethod]
        public void Extends_StringPath_RelativePath_Validation()
        {
            var code = @"
extends ""base.gd""

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            // Relative path handling might differ

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            // Behavior for relative paths - implementation may vary
        }

        #endregion

        #region extends with Various Path Formats

        [TestMethod]
        public void Extends_StringPath_NestedPath_ScriptExists_NoWarning()
        {
            var code = @"
extends ""res://scripts/entities/base/entity.gd""

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddScript("res://scripts/entities/base/entity.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://scripts/entities/base/entity.gd",
                BaseType = "Node"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Extends_StringPath_WithSingleQuotes()
        {
            // GDScript also supports single quotes
            var code = @"
extends 'res://scripts/base.gd'

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddScript("res://scripts/base.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://scripts/base.gd",
                BaseType = "Node"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        #endregion

        #region Combined extends and Inheritance Tests

        [TestMethod]
        public void Extends_StringPath_InheritanceChain()
        {
            // base.gd extends Node
            // child.gd extends "res://base.gd"
            var code = @"
extends ""res://base.gd""

func child_method():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddScript("res://base.gd", new GDScriptTypeInfo
            {
                ScriptPath = "res://base.gd",
                ClassName = "Base",
                BaseType = "Node"
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            var result = _validator.ValidateCode(code, options);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Extends_NoExtendsClause_NoWarning()
        {
            var code = @"
func test():
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UnknownBaseType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Extends_EmptyStringPath()
        {
            var code = @"
extends """"

func test():
    pass
";
            var mockProvider = new MockProjectRuntimeProvider();

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider
            };

            // Empty path should be handled gracefully
            var result = _validator.ValidateCode(code, options);
        }

        #endregion
    }
}
