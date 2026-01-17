using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for global enum constants recognition (KEY_SPACE, KEY_ENTER, MOUSE_BUTTON_LEFT, etc.)
    /// without requiring the enum prefix (Key.KEY_SPACE).
    /// </summary>
    [TestClass]
    public class GlobalEnumConstantsTests
    {
        private GDValidator _validator;
        private GDGodotTypesProvider _godotProvider;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
            _godotProvider = new GDGodotTypesProvider();
        }

        [TestMethod]
        public void GlobalEnumConstant_KeySpace_NoUndefinedError()
        {
            var code = @"
extends Node

func test():
    var space_key = KEY_SPACE
    var enter_key = KEY_ENTER
    print(space_key, enter_key)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           (e.Message.Contains("KEY_SPACE") || e.Message.Contains("KEY_ENTER")))
                .Should().BeEmpty("KEY_SPACE and KEY_ENTER should be recognized as global enum constants");
        }

        [TestMethod]
        public void GlobalEnumConstant_WithPrefix_Works()
        {
            var code = @"
extends Node

func test():
    var key = Key.KEY_SPACE
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable && e.Message.Contains("KEY_SPACE"))
                .Should().BeEmpty();
        }

        [TestMethod]
        public void GlobalEnumConstant_MouseButton_NoUndefinedError()
        {
            var code = @"
extends Node

func test():
    var left = MOUSE_BUTTON_LEFT
    var right = MOUSE_BUTTON_RIGHT
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           (e.Message.Contains("MOUSE_BUTTON_LEFT") || e.Message.Contains("MOUSE_BUTTON_RIGHT")))
                .Should().BeEmpty("MOUSE_BUTTON_* should be recognized as global enum constants");
        }

        [TestMethod]
        public void GlobalEnumConstant_ErrorCodes_NoUndefinedError()
        {
            var code = @"
extends Node

func test():
    var ok = OK
    var failed = FAILED
    var not_found = ERR_FILE_NOT_FOUND
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckScope = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.UndefinedVariable &&
                           (e.Message.Contains("OK") || e.Message.Contains("FAILED") || e.Message.Contains("ERR_FILE_NOT_FOUND")))
                .Should().BeEmpty("Error enum constants should be recognized");
        }

        [TestMethod]
        public void IsBuiltIn_KeySpace_ReturnsTrue()
        {
            _godotProvider.IsBuiltIn("KEY_SPACE").Should().BeTrue("KEY_SPACE should be recognized as built-in");
        }

        [TestMethod]
        public void IsBuiltIn_KeyEnter_ReturnsTrue()
        {
            _godotProvider.IsBuiltIn("KEY_ENTER").Should().BeTrue("KEY_ENTER should be recognized as built-in");
        }

        [TestMethod]
        public void IsBuiltIn_MouseButtonLeft_ReturnsTrue()
        {
            _godotProvider.IsBuiltIn("MOUSE_BUTTON_LEFT").Should().BeTrue("MOUSE_BUTTON_LEFT should be recognized as built-in");
        }

        [TestMethod]
        public void IsBuiltIn_JoyButtonA_ReturnsTrue()
        {
            _godotProvider.IsBuiltIn("JOY_BUTTON_A").Should().BeTrue("JOY_BUTTON_A should be recognized as built-in");
        }
    }
}
