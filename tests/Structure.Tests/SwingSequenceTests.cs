using System;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    public class SwingSequenceTests
    {
        [Theory]
        [InlineData(100.0, 110.0, TrendLabel.HH)]
        [InlineData(100.0, 90.0, TrendLabel.LH)]
        [InlineData(100.0, 100.0, TrendLabel.Undetermined)]
        public void SecondHigh_IsLabelledAgainstTheFirst(
            double first, double second, TrendLabel expected)
        {
            var sequence = new SwingSequence();
            Swing later = TestBars.High(20, second);

            sequence.Add(TestBars.High(10, first));
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
            Swing later = TestBars.Low(20, second);

            sequence.Add(TestBars.Low(10, first));
            sequence.Add(later);

            Assert.Equal(expected, later.Label);
        }

        [Fact]
        public void FirstSwingOfEachKind_HasNoPredecessorSoStaysUndetermined()
        {
            var sequence = new SwingSequence();
            Swing firstHigh = TestBars.High(10, 100.0);
            Swing firstLow = TestBars.Low(11, 90.0);

            sequence.Add(firstHigh);
            sequence.Add(firstLow);

            Assert.Equal(TrendLabel.Undetermined, firstHigh.Label);
            Assert.Equal(TrendLabel.Undetermined, firstLow.Label);
        }

        [Fact]
        public void HighsAndLowsAreComparedIndependently()
        {
            var sequence = new SwingSequence();
            Swing secondHigh = TestBars.High(30, 110.0);
            Swing secondLow = TestBars.Low(40, 80.0);

            // Rising highs, falling lows: a broadening move.
            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.Low(20, 90.0));
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
            Swing highFirst = TestBars.High(30, 110.0);
            Swing lowSecond = TestBars.Low(30, 80.0);

            var a = new SwingSequence();
            a.Add(TestBars.High(10, 100.0));
            a.Add(TestBars.Low(10, 90.0));
            a.Add(highFirst);
            a.Add(lowSecond);

            Swing lowFirst = TestBars.Low(30, 80.0);
            Swing highSecond = TestBars.High(30, 110.0);

            var b = new SwingSequence();
            b.Add(TestBars.High(10, 100.0));
            b.Add(TestBars.Low(10, 90.0));
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

            Swing high = TestBars.High(10, 100.0);
            sequence.Add(high);
            Assert.Same(high, sequence.LastHigh);
            Assert.Null(sequence.LastLow);

            Swing low = TestBars.Low(20, 90.0);
            sequence.Add(low);
            Assert.Same(high, sequence.LastHigh);
            Assert.Same(low, sequence.LastLow);

            Swing newerHigh = TestBars.High(30, 120.0);
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
