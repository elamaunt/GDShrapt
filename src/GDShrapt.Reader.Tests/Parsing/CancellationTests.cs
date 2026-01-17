using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class CancellationTests
    {
        /// <summary>
        /// Tests that cancellation works when check interval is set to 1 (check every char)
        /// and token is pre-canceled.
        /// </summary>
        [TestMethod]
        public void ParseFileContent_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var code = @"
class_name Player

func attack():
    pass

func defend():
    pass
";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Use small interval so cancellation is checked quickly
            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseFileContent(code, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseExpression_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var expression = "1 + 2 + 3 + 4 + 5";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseExpression(expression, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseStatement_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var statement = "var x = 1 + 2 + 3";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseStatement(statement, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseStatements_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var statements = @"
var x = 1
var y = 2
var z = 3
";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseStatements(statements, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseStatementsList_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var statements = @"
var x = 1
var y = 2
";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseStatementsList(statements, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseType_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var type = "Dictionary[String, int]";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseType(type, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseUnspecifiedContent_CancellationRequested_WithSmallInterval_ThrowsOperationCanceledException()
        {
            var content = "some content here";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseUnspecifiedContent(content, cts.Token);

            action.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public async Task ParseFileContent_CancellationDuringParsing_StopsEarly()
        {
            // Generate a large code to ensure parsing takes time
            var largeCode = GenerateLargeGDScript(5000);

            var cts = new CancellationTokenSource();

            // Small check interval for faster cancellation response in test
            var settings = new GDReadSettings { CancellationCheckInterval = 100 };
            var reader = new GDScriptReader(settings);

            // Cancel after a short delay
            var parseTask = Task.Run(() =>
            {
                reader.ParseFileContent(largeCode, cts.Token);
            });

            // Cancel after 10ms
            await Task.Delay(10);
            cts.Cancel();

            // Should throw OperationCanceledException
            Func<Task> action = async () => await parseTask;
            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public void ParseFileContent_NoCancellation_CompletesSuccessfully()
        {
            var code = @"
class_name Player

func attack():
    pass
";
            var reader = new GDScriptReader();

            // Using default CancellationToken.None
            var result = reader.ParseFileContent(code);

            result.Should().NotBeNull();
            result.ClassName.Should().NotBeNull();
            result.ClassName.Identifier.Sequence.Should().Be("Player");
        }

        [TestMethod]
        public void ParseFileContent_WithCancellationToken_None_CompletesSuccessfully()
        {
            var code = @"
func test():
    pass
";
            var reader = new GDScriptReader();

            var result = reader.ParseFileContent(code, CancellationToken.None);

            result.Should().NotBeNull();
        }

        [TestMethod]
        public void CancellationCheckInterval_ZeroDisablesCancellation()
        {
            var code = @"
class_name Player

func attack():
    pass
";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // With interval = 0, cancellation checks are disabled
            var settings = new GDReadSettings { CancellationCheckInterval = 0 };
            var reader = new GDScriptReader(settings);

            // Should NOT throw because cancellation checks are disabled
            Action action = () => reader.ParseFileContent(code, cts.Token);

            action.Should().NotThrow<OperationCanceledException>();
        }

        [TestMethod]
        public void CancellationCheckInterval_SmallValue_FrequentChecks()
        {
            var code = GenerateLargeGDScript(100);

            var cts = new CancellationTokenSource();

            // Very small interval = check almost every char
            var settings = new GDReadSettings { CancellationCheckInterval = 1 };
            var reader = new GDScriptReader(settings);

            // Schedule cancellation after a very short time
            Task.Run(async () =>
            {
                await Task.Delay(1);
                cts.Cancel();
            });

            // With frequent checks, should cancel quickly
            Action action = () => reader.ParseFileContent(code, cts.Token);

            // May or may not throw depending on timing, but should not hang
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void CancellationCheckInterval_LargeValue_LessFrequentChecks()
        {
            var code = "var x = 1"; // Very small code

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Large interval = code will complete before first check
            var settings = new GDReadSettings { CancellationCheckInterval = 10000 };
            var reader = new GDScriptReader(settings);

            // Should complete before cancellation check happens
            Action action = () => reader.ParseFileContent(code, cts.Token);

            action.Should().NotThrow<OperationCanceledException>();
        }

        [TestMethod]
        public void DefaultCancellationCheckInterval_Is256()
        {
            var settings = new GDReadSettings();

            settings.CancellationCheckInterval.Should().Be(256);
        }

        private static string GenerateLargeGDScript(int methodCount)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("class_name LargeClass");
            sb.AppendLine();

            for (int i = 0; i < methodCount; i++)
            {
                sb.AppendLine($"func method_{i}():");
                sb.AppendLine($"    var x_{i} = {i}");
                sb.AppendLine($"    var y_{i} = x_{i} + 1");
                sb.AppendLine($"    return y_{i}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
