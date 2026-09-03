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
        private int _lastBarIndex;
        private bool _hasBar;

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
        /// Returns the detector to its just constructed state. NinjaTrader
        /// re-initialises indicators on reload, parameter change and data refresh,
        /// and the next series may start at a different bar index.
        /// </summary>
        public void Reset()
        {
            Array.Clear(_window, 0, _window.Length);
            _barsSeen = 0;
            _writeIndex = 0;
            _lastBarIndex = 0;
            _hasBar = false;
        }

        /// <summary>
        /// Feed the next bar, identified by the caller's own bar index. Returns
        /// whatever this bar confirmed, which is usually nothing. A single bar can
        /// confirm both a high and a low when it engulfs its neighbours on both
        /// sides.
        /// </summary>
        /// <remarks>
        /// The index comes from the caller rather than an internal count because a
        /// host may not start at bar zero, and reported pivots have to line up with
        /// the host's own numbering.
        /// </remarks>
        public PivotResult OnBar(int barIndex, Bar bar)
        {
            if (barIndex < 0)
                throw new ArgumentOutOfRangeException("barIndex");

            if (_hasBar)
            {
                // NinjaScript calls OnBarUpdate repeatedly for the forming bar
                // unless Calculate is OnBarClose. Reprocessing would corrupt the
                // window, and re-emitting a pivot already reported would repaint.
                if (barIndex == _lastBarIndex)
                    return new PivotResult(null, null);

                if (barIndex != _lastBarIndex + 1)
                    throw new ArgumentOutOfRangeException(
                        "barIndex", "Bars must arrive in order with no gaps.");
            }

            _lastBarIndex = barIndex;
            _hasBar = true;

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

            int candidateIndex = barIndex - _swingStrength;

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
