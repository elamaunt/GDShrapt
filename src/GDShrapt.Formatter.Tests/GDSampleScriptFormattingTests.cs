using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Round-trip formatting tests using all sample GDScript files.
    /// These tests verify that the formatter doesn't break code structure.
    /// </summary>
    [TestClass]
    public class GDSampleScriptFormattingTests
    {
        #region Basic Samples

        [TestMethod]
        public void Sample_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample.gd");
        }

        [TestMethod]
        public void Sample2_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample2.gd");
        }

        [TestMethod]
        public void Sample3_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample3.gd");
        }

        [TestMethod]
        public void Sample4_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample4.gd");
        }

        [TestMethod]
        public void Sample5_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample5.gd");
        }

        #endregion

        #region Advanced Samples

        [TestMethod]
        public void Sample6_InnerClasses_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample6_InnerClasses.gd");
        }

        [TestMethod]
        public void Sample7_Lambdas_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample7_Lambdas.gd");
        }

        [TestMethod]
        public void Sample8_MatchPatterns_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample8_MatchPatterns.gd");
        }

        [TestMethod]
        public void Sample9_Properties_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample9_Properties.gd");
        }

        [TestMethod]
        public void Sample10_Operators_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample10_Operators.gd");
        }

        [TestMethod]
        public void Sample11_Annotations_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample11_Annotations.gd");
        }

        [TestMethod]
        public void Sample12_Signals_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample12_Signals.gd");
        }

        [TestMethod]
        public void Sample13_TypeSystem_RoundTrip()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample13_TypeSystem.gd");
        }

        #endregion

        #region Format Idempotency

        [TestMethod]
        public void AllSamples_FormatIdempotent()
        {
            foreach (var script in GDRoundTripTestHelper.GetAllSampleScripts())
            {
                try
                {
                    GDRoundTripTestHelper.AssertSampleScriptRoundTrip(script);
                }
                catch (System.Exception ex)
                {
                    Assert.Fail($"Script {script} failed round-trip test: {ex.Message}");
                }
            }
        }

        #endregion

        #region Format Options Variants

        [TestMethod]
        public void Sample_RoundTrip_WithSpacesIndent()
        {
            var options = new GDFormatterOptions
            {
                IndentStyle = IndentStyle.Spaces,
                IndentSize = 4
            };
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample.gd", options);
        }

        [TestMethod]
        public void Sample_RoundTrip_WithMinimalOptions()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample.gd", GDFormatterOptions.Minimal);
        }

        [TestMethod]
        public void Sample7_Lambdas_RoundTrip_WithStyleGuide()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample7_Lambdas.gd", GDFormatterOptions.GDScriptStyleGuide);
        }

        [TestMethod]
        public void Sample8_MatchPatterns_RoundTrip_WithStyleGuide()
        {
            GDRoundTripTestHelper.AssertSampleScriptRoundTrip("Sample8_MatchPatterns.gd", GDFormatterOptions.GDScriptStyleGuide);
        }

        #endregion
    }
}
