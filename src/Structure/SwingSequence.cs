using System;

namespace PkStructure
{
    /// <summary>
    /// Consumes confirmed swings in order and labels each one against the previous
    /// swing of the same kind. Highs and lows are tracked independently.
    /// </summary>
    public sealed class SwingSequence
    {
        private Swing _lastHigh;
        private Swing _lastLow;

        /// <summary>Most recent confirmed swing high, or null if none yet.</summary>
        public Swing LastHigh { get { return _lastHigh; } }

        /// <summary>Most recent confirmed swing low, or null if none yet.</summary>
        public Swing LastLow { get { return _lastLow; } }

        /// <summary>
        /// Derived on access rather than stored, so it cannot fall out of step with
        /// the swings. Both sides must agree: higher highs on lower lows is a
        /// broadening move, not an uptrend, and is reported as undetermined.
        /// </summary>
        public TrendState State
        {
            get
            {
                if (_lastHigh == null || _lastLow == null)
                    return TrendState.Undetermined;

                if (_lastHigh.Label == TrendLabel.HH && _lastLow.Label == TrendLabel.HL)
                    return TrendState.Uptrend;

                if (_lastHigh.Label == TrendLabel.LH && _lastLow.Label == TrendLabel.LL)
                    return TrendState.Downtrend;

                return TrendState.Undetermined;
            }
        }

        /// <summary>
        /// Labels the swing and takes it as the new reference for its kind. Swings
        /// must arrive in confirmation order, which is what PivotDetector emits.
        /// </summary>
        public void Add(Swing swing)
        {
            if (swing == null)
                throw new ArgumentNullException("swing");

            if (swing.Kind == SwingKind.High)
            {
                swing.Label = Classify(_lastHigh, swing, TrendLabel.HH, TrendLabel.LH);
                _lastHigh = swing;
            }
            else
            {
                swing.Label = Classify(_lastLow, swing, TrendLabel.HL, TrendLabel.LL);
                _lastLow = swing;
            }
        }

        /// <summary>
        /// Exact comparison is intended. Prices come from bar data as discrete tick
        /// values, and an equal high is a double top, not a higher high.
        /// </summary>
        private static TrendLabel Classify(
            Swing previous, Swing current, TrendLabel higher, TrendLabel lower)
        {
            if (previous == null)
                return TrendLabel.Undetermined;

            if (current.Price > previous.Price)
                return higher;

            if (current.Price < previous.Price)
                return lower;

            return TrendLabel.Undetermined;
        }
    }
}
