using System;
using System.Collections.Generic;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    /// <summary>
    /// The product's central claim: what the detector said about the past never
    /// changes when more bars arrive.
    /// </summary>
    public class NonRepaintingTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void EveryPrefixProducesAPrefixOfTheFullRun(int swingStrength)
        {
            const int BarCount = 200;
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<Confirmation> full = TestBars.Feed(
                new PivotDetector(swingStrength), highs, lows);

            Assert.NotEmpty(full);

            // Replaying only the first n bars must reproduce exactly the
            // confirmations the full run made at bar indices below n, and nothing
            // else. If a later bar could revise or retract an earlier call, this
            // fails.
            for (int n = 1; n <= BarCount; n++)
            {
                List<Confirmation> prefix = TestBars.Feed(
                    new PivotDetector(swingStrength), highs, lows, n);

                var expected = new List<Confirmation>();
                for (int i = 0; i < full.Count; i++)
                {
                    if (full[i].BarIndex < n)
                        expected.Add(full[i]);
                }

                Assert.Equal(expected.Count, prefix.Count);

                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.Equal(expected[i].BarIndex, prefix[i].BarIndex);
                    SwingAssert.SameSwing(expected[i].PivotHigh, prefix[i].PivotHigh);
                    SwingAssert.SameSwing(expected[i].PivotLow, prefix[i].PivotLow);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void NoSwingIsReportedBeforeTheBarThatConfirmsIt(int swingStrength)
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(200, out highs, out lows);

            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(swingStrength), highs, lows);

            Assert.NotEmpty(confirmations);

            foreach (Confirmation confirmation in confirmations)
            {
                AssertConfirmedHere(confirmation.PivotHigh, confirmation.BarIndex, swingStrength);
                AssertConfirmedHere(confirmation.PivotLow, confirmation.BarIndex, swingStrength);
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 4)]
        [InlineData(3, 6)]
        [InlineData(10, 20)]
        public void SeriesShorterThanTheWindowConfirmsNothing(int swingStrength, int barCount)
        {
            // barCount is always one short of the 2 * strength + 1 window.
            var highs = new double[barCount];
            var lows = new double[barCount];
            for (int i = 0; i < barCount; i++)
            {
                highs[i] = 100.0 + i;
                lows[i] = 50.0 - i;
            }

            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(swingStrength), highs, lows, barCount);

            Assert.Empty(confirmations);
        }

        [Fact]
        public void PlateauOfEqualHighsProducesNoPivot()
        {
            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(1),
                new double[] { 10, 20, 20, 20, 10 },
                new double[] { 1, 5, 5, 5, 1 });

            Assert.Empty(confirmations);
        }

        [Fact]
        public void FlatSeriesProducesNoSwings()
        {
            const int BarCount = 50;
            var highs = new double[BarCount];
            var lows = new double[BarCount];
            for (int i = 0; i < BarCount; i++)
            {
                highs[i] = 100.0;
                lows[i] = 99.0;
            }

            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(2), highs, lows, BarCount);

            Assert.Empty(confirmations);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void SwingStrengthBelowOne_IsRejectedAtConstruction(int swingStrength)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PivotDetector(swingStrength));
        }

        private static void AssertConfirmedHere(Swing swing, int barIndex, int swingStrength)
        {
            if (swing == null)
                return;

            Assert.Equal(barIndex, swing.ConfirmedAtIndex);
            Assert.Equal(barIndex - swingStrength, swing.Index);
        }

    }
}
