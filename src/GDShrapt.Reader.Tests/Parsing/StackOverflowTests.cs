using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class StackOverflowTests
    {
        [TestMethod]
        public void DeeplyNestedExpression_ThrowsGDStackOverflowException()
        {
            // Create deeply nested expression that exceeds default stack limit (64)
            var nestedExpr = "((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((1))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))";

            var reader = new GDScriptReader();

            // Should throw GDStackOverflowException, not StackOverflowException
            Action action = () => reader.ParseExpression(nestedExpr);

            action.Should().Throw<GDStackOverflowException>()
                .Where(e => e.OverflowType == GDStackOverflowType.ReadingStack)
                .Where(e => e.MaxDepth == 64)
                .Where(e => e.CurrentDepth >= 64);
        }

        [TestMethod]
        public void CustomMaxReadingStack_RespectsLimit()
        {
            var nestedExpr = "((((((((((1))))))))))"; // 10 levels of nesting

            var settings = new GDReadSettings { MaxReadingStack = 5 };
            var reader = new GDScriptReader(settings);

            Action action = () => reader.ParseExpression(nestedExpr);

            action.Should().Throw<GDStackOverflowException>()
                .Where(e => e.MaxDepth == 5);
        }

        [TestMethod]
        public void NormalNesting_DoesNotThrow()
        {
            var nestedExpr = "((((1))))"; // 4 levels - should be fine

            var reader = new GDScriptReader();

            Action action = () => reader.ParseExpression(nestedExpr);

            action.Should().NotThrow<GDStackOverflowException>();
        }

        [TestMethod]
        public void ExceptionMessage_ContainsHelpfulInfo()
        {
            var nestedExpr = "((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((1))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))";

            var reader = new GDScriptReader();

            try
            {
                reader.ParseExpression(nestedExpr);
                Assert.Fail("Expected GDStackOverflowException");
            }
            catch (GDStackOverflowException ex)
            {
                ex.Message.Should().Contain("Maximum reading stack depth exceeded");
                ex.Message.Should().Contain("MaxReadingStack");
                ex.Message.Should().Contain("GDReadSettings");
            }
        }

        [TestMethod]
        public void DisabledLimit_DoesNotThrow()
        {
            var nestedExpr = "((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((1))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))";

            var settings = new GDReadSettings { MaxReadingStack = null };
            var reader = new GDScriptReader(settings);

            // With disabled limit, parsing should proceed (or throw real StackOverflow in extreme cases)
            // For this test, we just verify it doesn't throw GDStackOverflowException
            try
            {
                reader.ParseExpression(nestedExpr);
            }
            catch (GDStackOverflowException)
            {
                Assert.Fail("Should not throw GDStackOverflowException when limit is disabled");
            }
            catch
            {
                // Other exceptions (like real StackOverflow) are acceptable in this test
            }
        }
    }
}
