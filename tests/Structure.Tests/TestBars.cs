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
        public static Bar Make(double high, double low)
        {
            var bar = new Bar();
            bar.Time = new DateTime(2026, 1, 1);
            bar.Open = low;
            bar.High = high;
            bar.Low = low;
            bar.Close = high;
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
