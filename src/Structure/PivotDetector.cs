using System;

namespace PkStructure
{
    /// <summary>
    /// Streaming pivot detection. Bars go in one at a time, in chart order, and a
    /// swing comes back on the bar that confirms it, never earlier.
    /// </summary>
    public sealed class PivotDetector
    {
        private readonly int _swingStrength;
        private readonly int _windowSize;
        private readonly Bar[] _window;

        private int _barsSeen;
        private int _writeIndex;

        public PivotDetector(int swingStrength)
        {
            if (swingStrength < 1)
                throw new ArgumentOutOfRangeException("swingStrength");

            _swingStrength = swingStrength;
            _windowSize = swingStrength * 2 + 1;
            _window = new Bar[_windowSize];
        }

        public int SwingStrength { get { return _swingStrength; } }

        /// <summary>
        /// Feed the next bar. Returns whatever this bar confirmed, which is usually
        /// nothing. A single bar can confirm both a high and a low when it engulfs
        /// its neighbours on both sides.
        /// </summary>
        public PivotResult OnBar(Bar bar)
        {
            _window[_writeIndex] = bar;
            _writeIndex = _writeIndex + 1 == _windowSize ? 0 : _writeIndex + 1;
            _barsSeen++;

            // A candidate needs SwingStrength bars on both sides, so nothing can be
            // judged until the window has filled.
            if (_barsSeen < _windowSize)
                return new PivotResult(null, null);

            // Buffer is full, so _writeIndex now points at the oldest bar and the
            // candidate sits SwingStrength slots along from it.
            int candidateSlot = Wrap(_writeIndex + _swingStrength);
            Bar candidate = _window[candidateSlot];

            bool isHigh = true;
            bool isLow = true;

            for (int offset = 0; offset < _windowSize; offset++)
            {
                if (offset == _swingStrength)
                    continue;

                Bar other = _window[Wrap(_writeIndex + offset)];

                // Strict comparison, so a plateau of equal highs yields no pivot.
                if (other.High >= candidate.High)
                    isHigh = false;
                if (other.Low <= candidate.Low)
                    isLow = false;

                if (!isHigh && !isLow)
                    return new PivotResult(null, null);
            }

            int candidateIndex = _barsSeen - 1 - _swingStrength;

            Swing pivotHigh = isHigh
                ? new Swing(candidateIndex, candidate.High, SwingKind.High, _swingStrength)
                : null;
            Swing pivotLow = isLow
                ? new Swing(candidateIndex, candidate.Low, SwingKind.Low, _swingStrength)
                : null;

            return new PivotResult(pivotHigh, pivotLow);
        }

        private int Wrap(int slot)
        {
            return slot >= _windowSize ? slot - _windowSize : slot;
        }
    }
}
