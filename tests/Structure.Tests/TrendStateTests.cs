using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    public class TrendStateTests
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

        [Fact]
        public void EmptySequence_IsUndetermined()
        {
            Assert.Equal(TrendState.Undetermined, new SwingSequence().State);
        }

        [Fact]
        public void OnlyHighs_IsUndetermined()
        {
            var sequence = new SwingSequence();

            sequence.Add(High(10, 100.0));
            sequence.Add(High(20, 110.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void OnlyLows_IsUndetermined()
        {
            var sequence = new SwingSequence();

            sequence.Add(Low(10, 90.0));
            sequence.Add(Low(20, 95.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void OneOfEachButNoPredecessors_IsUndetermined()
        {
            var sequence = new SwingSequence();

            sequence.Add(High(10, 100.0));
            sequence.Add(Low(20, 90.0));

            // Both are still Undetermined, so no trend can be claimed.
            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void HigherHighAndHigherLow_IsUptrend()
        {
            var sequence = new SwingSequence();

            sequence.Add(High(10, 100.0));
            sequence.Add(Low(20, 90.0));
            sequence.Add(High(30, 110.0));
            sequence.Add(Low(40, 95.0));

            Assert.Equal(TrendState.Uptrend, sequence.State);
        }

        [Fact]
        public void LowerHighAndLowerLow_IsDowntrend()
        {
            var sequence = new SwingSequence();

            sequence.Add(High(10, 110.0));
            sequence.Add(Low(20, 95.0));
            sequence.Add(High(30, 100.0));
            sequence.Add(Low(40, 90.0));

            Assert.Equal(TrendState.Downtrend, sequence.State);
        }

        [Fact]
        public void HigherHighOnLowerLow_IsUndetermined()
        {
            var sequence = new SwingSequence();

            // A broadening formation. Neither side agrees, so no direction.
            sequence.Add(High(10, 100.0));
            sequence.Add(Low(20, 90.0));
            sequence.Add(High(30, 110.0));
            sequence.Add(Low(40, 80.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void LowerHighOnHigherLow_IsUndetermined()
        {
            var sequence = new SwingSequence();

            // A contracting triangle. Also no direction.
            sequence.Add(High(10, 110.0));
            sequence.Add(Low(20, 90.0));
            sequence.Add(High(30, 100.0));
            sequence.Add(Low(40, 95.0));

            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void EqualHighBreaksAnOtherwiseValidUptrend()
        {
            var sequence = new SwingSequence();

            sequence.Add(High(10, 100.0));
            sequence.Add(Low(20, 90.0));
            sequence.Add(High(30, 100.0));
            sequence.Add(Low(40, 95.0));

            // Double top: the high side is Undetermined, so the trend is too.
            Assert.Equal(TrendState.Undetermined, sequence.State);
        }

        [Fact]
        public void TrendFlipsWhenLaterSwingsReverseTheSequence()
        {
            var sequence = new SwingSequence();

            sequence.Add(High(10, 100.0));
            sequence.Add(Low(20, 90.0));
            sequence.Add(High(30, 110.0));
            sequence.Add(Low(40, 95.0));
            Assert.Equal(TrendState.Uptrend, sequence.State);

            sequence.Add(High(50, 105.0));
            sequence.Add(Low(60, 85.0));
            Assert.Equal(TrendState.Downtrend, sequence.State);
        }
    }
}
