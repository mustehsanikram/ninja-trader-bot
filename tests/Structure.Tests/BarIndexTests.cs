using System;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    /// <summary>
    /// How the detector handles the bar index the caller supplies: repeats,
    /// non-zero starts, and out of order arrivals.
    /// </summary>
    public class BarIndexTests
    {
        // Strength 1, so bar 1 is a pivot high confirmed by bar 2.
        private static readonly double[] Highs = { 10.0, 20.0, 10.0 };
        private static readonly double[] Lows = { 5.0, 15.0, 5.0 };

        private static PivotResult FeedAt(PivotDetector detector, int barIndex, int sample)
        {
            return detector.OnBar(barIndex, TestBars.Make(Highs[sample], Lows[sample]));
        }

        [Fact]
        public void RepeatingTheSameBarIndexIsIgnored()
        {
            var detector = new PivotDetector(1);

            FeedAt(detector, 0, 0);
            FeedAt(detector, 1, 1);
            PivotResult first = FeedAt(detector, 2, 2);

            // NinjaScript calls OnBarUpdate repeatedly for the forming bar unless
            // Calculate is OnBarClose. The repeat must change nothing.
            PivotResult repeat = FeedAt(detector, 2, 2);

            Assert.NotNull(first.PivotHigh);
            Assert.Equal(1, first.PivotHigh.Index);
            Assert.False(repeat.HasAny);
        }

        [Fact]
        public void ManyRepeatsLeaveTheDetectorAbleToContinue()
        {
            var detector = new PivotDetector(1);

            FeedAt(detector, 0, 0);
            for (int i = 0; i < 50; i++)
                FeedAt(detector, 1, 1);

            PivotResult confirmed = FeedAt(detector, 2, 2);

            // Fifty ticks on bar 1 must leave the window exactly as one bar would.
            Assert.NotNull(confirmed.PivotHigh);
            Assert.Equal(1, confirmed.PivotHigh.Index);
            Assert.Equal(2, confirmed.PivotHigh.ConfirmedAtIndex);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(75000)]
        public void PivotIndicesUseTheCallersNumbering(int start)
        {
            var detector = new PivotDetector(1);

            FeedAt(detector, start, 0);
            FeedAt(detector, start + 1, 1);
            PivotResult result = FeedAt(detector, start + 2, 2);

            // A host that does not begin at bar zero must still get pivots that
            // line up with its own bar numbering.
            Assert.NotNull(result.PivotHigh);
            Assert.Equal(start + 1, result.PivotHigh.Index);
            Assert.Equal(start + 2, result.PivotHigh.ConfirmedAtIndex);
        }

        [Fact]
        public void GoingBackwardsIsRejected()
        {
            var detector = new PivotDetector(1);

            FeedAt(detector, 10, 0);
            FeedAt(detector, 11, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => FeedAt(detector, 10, 2));
        }

        [Fact]
        public void SkippingABarIsRejected()
        {
            var detector = new PivotDetector(1);

            FeedAt(detector, 0, 0);

            // A gap would silently break the pivot arithmetic, because the window
            // assumes its bars are adjacent.
            Assert.Throws<ArgumentOutOfRangeException>(() => FeedAt(detector, 2, 1));
        }

        [Fact]
        public void NegativeBarIndexIsRejected()
        {
            var detector = new PivotDetector(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => FeedAt(detector, -1, 0));
        }
    }
}
