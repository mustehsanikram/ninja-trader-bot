using System;
using System.Collections.Generic;
using PkStructure;

namespace PkStructure.Tests
{
    /// <summary>What the detector reported, and which bar reported it.</summary>
    internal struct Confirmation
    {
        public int BarIndex;
        public Swing PivotHigh;
        public Swing PivotLow;
    }

    internal static class TestBars
    {
        /// <summary>Default SwingStrength for hand-built swings.</summary>
        public const int Strength = 3;

        public static Swing High(int index, double price)
        {
            return new Swing(index, price, SwingKind.High, Strength);
        }

        public static Swing Low(int index, double price)
        {
            return new Swing(index, price, SwingKind.Low, Strength);
        }

        /// <summary>
        /// A bar with no opinion about its open or close: both sit at the midpoint,
        /// strictly inside the range. This form can never satisfy a
        /// close-beyond-a-level condition, which is why it rejects a range it
        /// cannot place a close inside. Use the four argument form whenever the
        /// close is what the test is about, or when the bar is flat.
        /// </summary>
        public static Bar Make(double high, double low)
        {
            if (high <= low)
                throw new ArgumentOutOfRangeException(
                    "high",
                    "This form needs high > low so the close can sit strictly inside "
                    + "the range. Use the four argument overload for a flat or "
                    + "deliberately shaped bar.");

            double middle = (high + low) / 2.0;
            return Make(middle, high, low, middle);
        }

        /// <summary>
        /// A bar stated in full. Allows high == low, which is how a flat bar gets
        /// built. Open and close are not range checked, so a caller can construct a
        /// deliberately impossible bar when that is the point of the test.
        /// </summary>
        public static Bar Make(double open, double high, double low, double close)
        {
            if (high < low)
                throw new ArgumentOutOfRangeException(
                    "high", "A bar cannot have a high below its low.");

            var bar = new Bar();
            bar.Time = new DateTime(2026, 1, 1);
            bar.Open = open;
            bar.High = high;
            bar.Low = low;
            bar.Close = close;
            return bar;
        }

        /// <summary>Feeds every bar in order and records what each one confirmed.</summary>
        public static List<Confirmation> Feed(
            PivotDetector detector, double[] highs, double[] lows)
        {
            return Feed(detector, highs, lows, highs.Length);
        }

        /// <summary>Feeds only the first barCount bars, for prefix replay comparisons.</summary>
        public static List<Confirmation> Feed(
            PivotDetector detector, double[] highs, double[] lows, int barCount)
        {
            var confirmations = new List<Confirmation>();

            for (int i = 0; i < barCount; i++)
            {
                PivotResult result = detector.OnBar(i, Make(highs[i], lows[i]));
                if (!result.HasAny)
                    continue;

                var confirmation = new Confirmation();
                confirmation.BarIndex = i;
                confirmation.PivotHigh = result.PivotHigh;
                confirmation.PivotLow = result.PivotLow;
                confirmations.Add(confirmation);
            }

            return confirmations;
        }

        /// <summary>A repeatable saw-tooth with pivots at varied spacing.</summary>
        public static void BuildSeries(int barCount, out double[] highs, out double[] lows)
        {
            highs = new double[barCount];
            lows = new double[barCount];

            for (int i = 0; i < barCount; i++)
            {
                // Deterministic and non-periodic enough to produce pivots at
                // irregular intervals rather than a fixed rhythm.
                double wave = Math.Sin(i * 0.7) * 10.0 + Math.Sin(i * 0.31) * 4.0;
                highs[i] = 100.0 + wave + 1.0;
                lows[i] = 100.0 + wave - 1.0;
            }
        }
    }
}
