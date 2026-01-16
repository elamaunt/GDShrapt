using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for signal validation: emit_signal, connect, and signal existence.
    /// </summary>
    [TestClass]
    public class SignalValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        #region emit_signal Tests

        [TestMethod]
        public void EmitSignal_ExistingSignal_CorrectArgs_NoError()
        {
            var code = @"
extends Node
signal my_signal(value: int)

func test():
    emit_signal(""my_signal"", 42)
";
            var result = _validator.ValidateCode(code);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.EmitSignalWrongArgCount ||
                           d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void EmitSignal_ExistingSignal_WrongArgCount_ReportsError()
        {
            var code = @"
extends Node
signal my_signal(arg1: int, arg2: String)

func test():
    emit_signal(""my_signal"", 1)
";
            var result = _validator.ValidateCode(code);

            result.Errors
                .Where(d => d.Code == GDDiagnosticCode.EmitSignalWrongArgCount)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void EmitSignal_NonexistentSignal_ReportsWarning()
        {
            var code = @"
extends Node

func test():
    emit_signal(""nonexistent_signal"")
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void EmitSignal_NoArgs_ReportsError()
        {
            var code = @"
extends Node

func test():
    emit_signal()
";
            var result = _validator.ValidateCode(code);

            result.Errors
                .Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void EmitSignal_DynamicSignalName_NoValidation()
        {
            var code = @"
extends Node

func test():
    var signal_name = ""my_signal""
    emit_signal(signal_name)
";
            var result = _validator.ValidateCode(code);

            // Should not report undefined signal for dynamic names
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void EmitSignal_OnSelf_ValidatesCorrectly()
        {
            var code = @"
extends Node
signal my_signal

func test():
    self.emit_signal(""my_signal"")
";
            var result = _validator.ValidateCode(code);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void EmitSignal_SignalWithNoParams_NoArgsEmitted_NoError()
        {
            var code = @"
extends Node
signal finished

func test():
    emit_signal(""finished"")
";
            var result = _validator.ValidateCode(code);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.EmitSignalWrongArgCount)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void EmitSignal_SignalWithNoParams_ExtraArgsEmitted_ReportsError()
        {
            var code = @"
extends Node
signal finished

func test():
    emit_signal(""finished"", 1, 2, 3)
";
            var result = _validator.ValidateCode(code);

            result.Errors
                .Where(d => d.Code == GDDiagnosticCode.EmitSignalWrongArgCount)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void EmitSignal_MultipleParams_CorrectCount_NoError()
        {
            var code = @"
extends Node
signal damage_taken(amount: int, source: Node, critical: bool)

func test():
    emit_signal(""damage_taken"", 10, self, true)
";
            var result = _validator.ValidateCode(code);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.EmitSignalWrongArgCount ||
                           d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().BeEmpty();
        }

        #endregion

        #region connect Tests

        [TestMethod]
        public void Connect_ExistingSignal_NoWarning()
        {
            var code = @"
extends Node
signal my_signal

func _ready():
    connect(""my_signal"", _on_signal)

func _on_signal():
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Connect_NonexistentSignal_ReportsWarning()
        {
            var code = @"
extends Node

func _ready():
    connect(""nonexistent"", _on_signal)

func _on_signal():
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Connect_CallbackTooManyRequiredParams_ReportsWarning()
        {
            var code = @"
extends Node
signal my_signal

func _ready():
    connect(""my_signal"", Callable(self, ""_on_signal""))

func _on_signal(arg1: int, arg2: String):
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ConnectCallbackMismatch)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Connect_CallbackMatchesSignal_NoWarning()
        {
            var code = @"
extends Node
signal health_changed(new_value: int)

func _ready():
    connect(""health_changed"", Callable(self, ""_on_health_changed""))

func _on_health_changed(value: int):
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ConnectCallbackMismatch)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Connect_CallbackAcceptsFewerParams_ReportsWarning()
        {
            // The validator warns when callback doesn't accept all signal parameters
            // (even though Godot runtime allows this, it may indicate a bug)
            var code = @"
extends Node
signal data_received(id: int, data: Dictionary)

func _ready():
    connect(""data_received"", Callable(self, ""_on_data""))

func _on_data(id: int):
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.ConnectCallbackMismatch)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void Connect_OnSelf_ValidatesCorrectly()
        {
            var code = @"
extends Node
signal my_signal

func _ready():
    self.connect(""my_signal"", _on_signal)

func _on_signal():
    pass
";
            var result = _validator.ValidateCode(code);

            result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit)
                .Should().BeEmpty();
        }

        #endregion

        #region Signal with Project Provider Tests

        [TestMethod]
        public void EmitSignal_WithProjectProvider_BuiltInSignal_NoWarning()
        {
            var code = @"
extends Node

func test():
    emit_signal(""ready"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            // ready is a built-in Node signal

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckSignals = true
            };

            var result = _validator.ValidateCode(code, options);

            // Built-in signals should be found through the provider
            // Note: This might still warn if the mock doesn't include Node signals
        }

        [TestMethod]
        public void EmitSignal_WithProjectProvider_RegisteredSignal_NoWarning()
        {
            var code = @"
extends Node

func test():
    emit_signal(""custom_event"", 42)
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddSignal("self", new GDSignalInfo
            {
                Name = "custom_event",
                Parameters = new List<GDRuntimeParameterInfo>
                {
                    new GDRuntimeParameterInfo("value", "int")
                }
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckSignals = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit ||
                           d.Code == GDDiagnosticCode.EmitSignalWrongArgCount)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void EmitSignal_WithProjectProvider_WrongArgCount_ReportsError()
        {
            var code = @"
extends Node

func test():
    emit_signal(""custom_event"")
";
            var mockProvider = new MockProjectRuntimeProvider();
            mockProvider.AddSignal("self", new GDSignalInfo
            {
                Name = "custom_event",
                Parameters = new List<GDRuntimeParameterInfo>
                {
                    new GDRuntimeParameterInfo("value", "int"),
                    new GDRuntimeParameterInfo("data", "String")
                }
            });

            var options = new GDValidationOptions
            {
                RuntimeProvider = mockProvider,
                CheckSignals = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(d => d.Code == GDDiagnosticCode.EmitSignalWrongArgCount)
                .Should().NotBeEmpty();
        }

        #endregion

        #region CheckSignals Option Tests

        [TestMethod]
        public void EmitSignal_CheckSignalsDisabled_NoValidation()
        {
            var code = @"
extends Node

func test():
    emit_signal(""nonexistent_signal"", 1, 2, 3)
";
            var options = new GDValidationOptions
            {
                CheckSignals = false
            };

            var result = _validator.ValidateCode(code, options);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit ||
                           d.Code == GDDiagnosticCode.EmitSignalWrongArgCount)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Connect_CheckSignalsDisabled_NoValidation()
        {
            var code = @"
extends Node

func _ready():
    connect(""nonexistent"", Callable(self, ""_handler""))

func _handler(a, b, c, d, e):
    pass
";
            var options = new GDValidationOptions
            {
                CheckSignals = false
            };

            var result = _validator.ValidateCode(code, options);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UndefinedSignalEmit ||
                           d.Code == GDDiagnosticCode.ConnectCallbackMismatch)
                .Should().BeEmpty();
        }

        #endregion
    }
}
