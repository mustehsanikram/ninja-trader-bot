using System;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    public class SwingTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(10, 3)]
        [InlineData(250, 7)]
        public void ConfirmedAtIndex_IsPivotIndexPlusSwingStrength(int index, int swingStrength)
        {
            var swing = new Swing(index, 100.0, SwingKind.High, swingStrength);

            Assert.Equal(index + swingStrength, swing.ConfirmedAtIndex);
            Assert.True(swing.ConfirmedAtIndex > swing.Index);
        }

        [Fact]
        public void NewSwing_HasNoTrendLabelYet()
        {
            var swing = new Swing(5, 100.0, SwingKind.Low, 3);

            Assert.Equal(TrendLabel.Undetermined, swing.Label);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SwingStrengthBelowOne_IsRejected(int swingStrength)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Swing(5, 100.0, SwingKind.High, swingStrength));
        }
    }
}
