using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    public class TrendStateTests
    {
        [Fact]
        public void EmptySequence_IsUndetermined()
        {
            Assert.Equal(TrendState.Undetermined, new SwingSequence().State);
        }

        [Fact]
        public void OnlyHighs_IsUndetermined()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.High(20, 110.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void OnlyLows_IsUndetermined()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.Low(10, 90.0));
            sequence.Add(TestBars.Low(20, 95.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void OneOfEachButNoPredecessors_IsUndetermined()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.Low(20, 90.0));

            // Both are still Undetermined, so no trend can be claimed.
            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void HigherHighAndHigherLow_IsUptrend()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.Low(20, 90.0));
            sequence.Add(TestBars.High(30, 110.0));
            sequence.Add(TestBars.Low(40, 95.0));

            Assert.Equal(TrendState.Uptrend, sequence.State);
        }

        [Fact]
        public void LowerHighAndLowerLow_IsDowntrend()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.High(10, 110.0));
            sequence.Add(TestBars.Low(20, 95.0));
            sequence.Add(TestBars.High(30, 100.0));
            sequence.Add(TestBars.Low(40, 90.0));

            Assert.Equal(TrendState.Downtrend, sequence.State);
        }

        [Fact]
        public void HigherHighOnLowerLow_IsUndetermined()
        {
            var sequence = new SwingSequence();

            // A broadening formation. Neither side agrees, so no direction.
            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.Low(20, 90.0));
            sequence.Add(TestBars.High(30, 110.0));
            sequence.Add(TestBars.Low(40, 80.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void LowerHighOnHigherLow_IsUndetermined()
        {
            var sequence = new SwingSequence();

            // A contracting triangle. Also no direction.
            sequence.Add(TestBars.High(10, 110.0));
            sequence.Add(TestBars.Low(20, 90.0));
            sequence.Add(TestBars.High(30, 100.0));
            sequence.Add(TestBars.Low(40, 95.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void EqualHighBreaksAnOtherwiseValidUptrend()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.Low(20, 90.0));
            sequence.Add(TestBars.High(30, 100.0));
            sequence.Add(TestBars.Low(40, 95.0));

            // Double top: the high side is Undetermined, so the trend is too.
            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void TrendFlipsWhenLaterSwingsReverseTheSequence()
        {
            var sequence = new SwingSequence();

            sequence.Add(TestBars.High(10, 100.0));
            sequence.Add(TestBars.Low(20, 90.0));
            sequence.Add(TestBars.High(30, 110.0));
            sequence.Add(TestBars.Low(40, 95.0));
            Assert.Equal(TrendState.Uptrend, sequence.State);

            sequence.Add(TestBars.High(50, 105.0));
            sequence.Add(TestBars.Low(60, 85.0));
            Assert.Equal(TrendState.Downtrend, sequence.State);
        }
    }
}
