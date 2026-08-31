using System.Collections.Generic;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    public class PivotDetectorTests
    {
        [Theory]
        // strength 1: one bar either side
        [InlineData(1, new double[] { 10, 20, 10 }, new double[] { 5, 15, 5 }, 1, 2)]
        // strength 2: two bars either side
        [InlineData(2, new double[] { 10, 12, 20, 12, 10 }, new double[] { 8, 9, 15, 9, 8 }, 2, 4)]
        // strength 3: three bars either side
        [InlineData(3, new double[] { 1, 2, 3, 10, 3, 2, 1 }, new double[] { 0, 1, 2, 9, 2, 1, 0 }, 3, 6)]
        public void PivotHigh_IsFoundAtTheExpectedBar_AndConfirmedLater(
            int swingStrength, double[] highs, double[] lows,
            int expectedPivotIndex, int expectedConfirmIndex)
        {
            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(swingStrength), highs, lows);

            Confirmation single = Assert.Single(confirmations);
            Assert.Equal(expectedConfirmIndex, single.BarIndex);

            Swing pivot = single.PivotHigh;
            Assert.NotNull(pivot);
            Assert.Equal(expectedPivotIndex, pivot.Index);
            Assert.Equal(SwingKind.High, pivot.Kind);
            Assert.Equal(highs[expectedPivotIndex], pivot.Price);

            // The confirming bar and the swing's own claim must agree.
            Assert.Equal(single.BarIndex, pivot.ConfirmedAtIndex);
        }

        [Fact]
        public void PivotLow_IsFoundIndependentlyOfHighs()
        {
            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(1),
                new double[] { 20, 10, 20 },
                new double[] { 15, 1, 15 });

            Confirmation single = Assert.Single(confirmations);
            Swing pivot = single.PivotLow;

            Assert.NotNull(pivot);
            Assert.Null(single.PivotHigh);
            Assert.Equal(1, pivot.Index);
            Assert.Equal(SwingKind.Low, pivot.Kind);
            Assert.Equal(1.0, pivot.Price);
        }

        [Fact]
        public void OneBarCanConfirmBothAHighAndALow_WhenItEngulfsItsNeighbours()
        {
            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(1),
                new double[] { 10, 20, 10 },
                new double[] { 5, 1, 5 });

            Confirmation single = Assert.Single(confirmations);

            Assert.NotNull(single.PivotHigh);
            Assert.NotNull(single.PivotLow);
            Assert.Equal(20.0, single.PivotHigh.Price);
            Assert.Equal(1.0, single.PivotLow.Price);
        }

        [Fact]
        public void ConsecutivePivots_AreEachConfirmedOnTheirOwnBar()
        {
            // highs peak at index 1 and again at index 3
            List<Confirmation> confirmations = TestBars.Feed(
                new PivotDetector(1),
                new double[] { 10, 20, 10, 30, 10 },
                new double[] { 9, 19, 2, 29, 9 });

            Assert.Equal(3, confirmations.Count);

            Assert.Equal(2, confirmations[0].BarIndex);
            Assert.Equal(1, confirmations[0].PivotHigh.Index);

            Assert.Equal(3, confirmations[1].BarIndex);
            Assert.Equal(2, confirmations[1].PivotLow.Index);

            Assert.Equal(4, confirmations[2].BarIndex);
            Assert.Equal(3, confirmations[2].PivotHigh.Index);
        }
    }
}
