using System;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    /// <summary>
    /// The bar builder is test infrastructure, but feature 3 onwards confirms
    /// breaks on the close, so what it puts there is load bearing.
    /// </summary>
    public class TestBarsTests
    {
        [Fact]
        public void FourArgumentForm_PreservesEveryValue()
        {
            Bar bar = TestBars.Make(101.0, 105.0, 99.0, 103.0);

            Assert.Equal(101.0, bar.Open);
            Assert.Equal(105.0, bar.High);
            Assert.Equal(99.0, bar.Low);
            Assert.Equal(103.0, bar.Close);
        }

        [Fact]
        public void FourArgumentForm_AllowsACloseAwayFromTheExtremes()
        {
            // The case the two argument form cannot express: price reached 110 but
            // closed back at 101, which is a sweep rather than a break.
            Bar swept = TestBars.Make(100.0, 110.0, 99.0, 101.0);

            Assert.Equal(110.0, swept.High);
            Assert.Equal(101.0, swept.Close);
            Assert.True(swept.Close < swept.High);
        }

        [Theory]
        [InlineData(110.0, 90.0)]
        [InlineData(100.0, 99.0)]
        [InlineData(20000.25, 19999.75)]
        public void TwoArgumentForm_PutsTheCloseStrictlyBetweenLowAndHigh(
            double high, double low)
        {
            Bar bar = TestBars.Make(high, low);

            // Pinning this stops anyone quietly restoring Close = high. A close on
            // the extreme would let a feature 3 test pass its "closed beyond the
            // level" assertion for the wrong reason.
            Assert.True(bar.Close > bar.Low, "close must sit above the low");
            Assert.True(bar.Close < bar.High, "close must sit below the high");
            Assert.Equal(bar.Open, bar.Close);
        }

        [Theory]
        [InlineData(100.0, 100.0)]   // zero range: no midpoint sits inside it
        [InlineData(90.0, 100.0)]    // inverted: not a bar at all
        [InlineData(0.0, 0.0)]
        public void TwoArgumentForm_RejectsARangeItCannotPlaceACloseInside(
            double high, double low)
        {
            // Refusing beats silently returning Close == High, which is the state
            // the midpoint exists to avoid.
            Assert.Throws<ArgumentOutOfRangeException>(() => TestBars.Make(high, low));
        }

        [Fact]
        public void FourArgumentForm_AcceptsAFlatBarAndKeepsTheCloseGiven()
        {
            // A bar with a single trade is a real shape. The caller states the
            // close instead of inheriting a misleading default.
            Bar flat = TestBars.Make(100.0, 100.0, 100.0, 100.0);

            Assert.Equal(100.0, flat.High);
            Assert.Equal(100.0, flat.Low);
            Assert.Equal(100.0, flat.Close);
        }

        [Fact]
        public void FourArgumentForm_RejectsAHighBelowItsLow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TestBars.Make(95.0, 90.0, 100.0, 95.0));
        }

        [Fact]
        public void TwoArgumentForm_StillReportsTheHighAndLowItWasGiven()
        {
            Bar bar = TestBars.Make(110.0, 90.0);

            // PivotDetector reads only these two, so they must stay exact.
            Assert.Equal(110.0, bar.High);
            Assert.Equal(90.0, bar.Low);
        }
    }
}
