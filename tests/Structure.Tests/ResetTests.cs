using System.Collections.Generic;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    /// <summary>
    /// Reset has to leave both classes indistinguishable from new. NinjaTrader
    /// re-initialises indicators on reload, parameter change and data refresh.
    /// </summary>
    public class ResetTests
    {
        private const int BarCount = 200;

        private static void AssertSameConfirmations(
            List<Confirmation> expected, List<Confirmation> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].BarIndex, actual[i].BarIndex);
                AssertSameSwing(expected[i].PivotHigh, actual[i].PivotHigh);
                AssertSameSwing(expected[i].PivotLow, actual[i].PivotLow);
            }
        }

        private static void AssertSameSwing(Swing expected, Swing actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.NotNull(actual);
            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Price, actual.Price);
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.ConfirmedAtIndex, actual.ConfirmedAtIndex);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void DetectorAfterResetMatchesAFreshInstance(int swingStrength)
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<Confirmation> fresh = TestBars.Feed(
                new PivotDetector(swingStrength), highs, lows);

            var reused = new PivotDetector(swingStrength);
            TestBars.Feed(reused, highs, lows);
            reused.Reset();
            List<Confirmation> afterReset = TestBars.Feed(reused, highs, lows);

            Assert.NotEmpty(fresh);
            AssertSameConfirmations(fresh, afterReset);
        }

        [Fact]
        public void DetectorAfterResetAcceptsADifferentStartingBarIndex()
        {
            var detector = new PivotDetector(1);

            detector.OnBar(500, TestBars.Make(10.0, 5.0));
            detector.OnBar(501, TestBars.Make(20.0, 15.0));
            detector.Reset();

            // A reload can hand back a series that starts anywhere. Without the
            // reset this would be rejected as going backwards.
            detector.OnBar(0, TestBars.Make(10.0, 5.0));
            detector.OnBar(1, TestBars.Make(20.0, 15.0));
            PivotResult result = detector.OnBar(2, TestBars.Make(10.0, 5.0));

            Assert.NotNull(result.PivotHigh);
            Assert.Equal(1, result.PivotHigh.Index);
            Assert.Equal(2, result.PivotHigh.ConfirmedAtIndex);
        }

        [Fact]
        public void DetectorResetOnAFreshInstanceChangesNothing()
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<Confirmation> fresh = TestBars.Feed(new PivotDetector(3), highs, lows);

            var reset = new PivotDetector(3);
            reset.Reset();
            List<Confirmation> afterReset = TestBars.Feed(reset, highs, lows);

            AssertSameConfirmations(fresh, afterReset);
        }

        [Fact]
        public void SequenceAfterResetLabelsAsIfNew()
        {
            var sequence = new SwingSequence();

            // Establish a downtrend, then reset and replay an uptrend.
            sequence.Add(new Swing(10, 110.0, SwingKind.High, 3));
            sequence.Add(new Swing(20, 95.0, SwingKind.Low, 3));
            sequence.Add(new Swing(30, 100.0, SwingKind.High, 3));
            sequence.Add(new Swing(40, 90.0, SwingKind.Low, 3));
            Assert.Equal(TrendState.Downtrend, sequence.State);

            sequence.Reset();

            Assert.Null(sequence.LastHigh);
            Assert.Null(sequence.LastLow);
            Assert.Equal(TrendState.Undetermined, sequence.State);

            Swing firstHigh = new Swing(50, 100.0, SwingKind.High, 3);
            sequence.Add(firstHigh);

            // Without the reset this high would be labelled against the 100.0 from
            // before and come out Undetermined for the wrong reason. After it, the
            // swing genuinely has no predecessor.
            Assert.Equal(TrendLabel.Undetermined, firstHigh.Label);
            Assert.Same(firstHigh, sequence.LastHigh);

            Swing secondHigh = new Swing(60, 120.0, SwingKind.High, 3);
            sequence.Add(secondHigh);
            Assert.Equal(TrendLabel.HH, secondHigh.Label);
        }

        [Fact]
        public void SequenceResetOnAFreshInstanceChangesNothing()
        {
            var sequence = new SwingSequence();
            sequence.Reset();

            Assert.Null(sequence.LastHigh);
            Assert.Null(sequence.LastLow);
            Assert.Equal(TrendState.Undetermined, sequence.State);
        }
    }
}
