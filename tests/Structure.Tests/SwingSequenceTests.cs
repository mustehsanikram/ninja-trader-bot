using System;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    public class SwingSequenceTests
    {
        private const int Strength = 3;

        private static Swing High(int index, double price)
        {
            return new Swing(index, price, SwingKind.High, Strength);
        }

        private static Swing Low(int index, double price)
        {
            return new Swing(index, price, SwingKind.Low, Strength);
        }

        [Theory]
        [InlineData(100.0, 110.0, TrendLabel.HH)]
        [InlineData(100.0, 90.0, TrendLabel.LH)]
        [InlineData(100.0, 100.0, TrendLabel.Undetermined)]
        public void SecondHigh_IsLabelledAgainstTheFirst(
            double first, double second, TrendLabel expected)
        {
            var sequence = new SwingSequence();
            Swing later = High(20, second);

            sequence.Add(High(10, first));
            sequence.Add(later);

            Assert.Equal(expected, later.Label);
        }

        [Theory]
        [InlineData(100.0, 110.0, TrendLabel.HL)]
        [InlineData(100.0, 90.0, TrendLabel.LL)]
        [InlineData(100.0, 100.0, TrendLabel.Undetermined)]
        public void SecondLow_IsLabelledAgainstTheFirst(
            double first, double second, TrendLabel expected)
        {
            var sequence = new SwingSequence();
            Swing later = Low(20, second);

            sequence.Add(Low(10, first));
            sequence.Add(later);

            Assert.Equal(expected, later.Label);
        }

        [Fact]
        public void FirstSwingOfEachKind_HasNoPredecessorSoStaysUndetermined()
        {
            var sequence = new SwingSequence();
            Swing firstHigh = High(10, 100.0);
            Swing firstLow = Low(11, 90.0);

            sequence.Add(firstHigh);
            sequence.Add(firstLow);

            Assert.Equal(TrendLabel.Undetermined, firstHigh.Label);
            Assert.Equal(TrendLabel.Undetermined, firstLow.Label);
        }

        [Fact]
        public void HighsAndLowsAreComparedIndependently()
        {
            var sequence = new SwingSequence();
            Swing secondHigh = High(30, 110.0);
            Swing secondLow = Low(40, 80.0);

            // Rising highs, falling lows: a broadening move.
            sequence.Add(High(10, 100.0));
            sequence.Add(Low(20, 90.0));
            sequence.Add(secondHigh);
            sequence.Add(secondLow);

            Assert.Equal(TrendLabel.HH, secondHigh.Label);
            Assert.Equal(TrendLabel.LL, secondLow.Label);
        }

        [Fact]
        public void AddOrderWithinABarDoesNotChangeLabels()
        {
            // An outside bar confirms a high and a low at once, so the caller may
            // add them in either order. Labels must not depend on that choice.
            Swing highFirst = High(30, 110.0);
            Swing lowSecond = Low(30, 80.0);

            var a = new SwingSequence();
            a.Add(High(10, 100.0));
            a.Add(Low(10, 90.0));
            a.Add(highFirst);
            a.Add(lowSecond);

            Swing lowFirst = Low(30, 80.0);
            Swing highSecond = High(30, 110.0);

            var b = new SwingSequence();
            b.Add(High(10, 100.0));
            b.Add(Low(10, 90.0));
            b.Add(lowFirst);
            b.Add(highSecond);

            Assert.Equal(highFirst.Label, highSecond.Label);
            Assert.Equal(lowSecond.Label, lowFirst.Label);
        }

        [Fact]
        public void LastHighAndLastLow_TrackTheMostRecentOfEachKind()
        {
            var sequence = new SwingSequence();

            Assert.Null(sequence.LastHigh);
            Assert.Null(sequence.LastLow);

            Swing high = High(10, 100.0);
            sequence.Add(high);
            Assert.Same(high, sequence.LastHigh);
            Assert.Null(sequence.LastLow);

            Swing low = Low(20, 90.0);
            sequence.Add(low);
            Assert.Same(high, sequence.LastHigh);
            Assert.Same(low, sequence.LastLow);

            Swing newerHigh = High(30, 120.0);
            sequence.Add(newerHigh);
            Assert.Same(newerHigh, sequence.LastHigh);
        }

        [Fact]
        public void NullSwing_IsRejected()
        {
            var sequence = new SwingSequence();

            Assert.Throws<ArgumentNullException>(() => sequence.Add(null));
        }
    }
}
