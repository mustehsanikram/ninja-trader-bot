using System.Collections.Generic;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    /// <summary>
    /// Bars through PivotDetector into SwingSequence, over a realistic series.
    /// Proves the two pieces compose and that the 1a guarantee survives the join.
    /// </summary>
    public class StructurePipelineTests
    {
        private const int BarCount = 200;

        private struct LabelledSwing
        {
            public int AddedAtBar;
            public int Index;
            public double Price;
            public SwingKind Kind;
            public TrendLabel Label;
            public int ConfirmedAtIndex;

            // The live object, kept so a later retroactive mutation is detectable.
            public Swing Instance;
        }

        private static List<LabelledSwing> Run(
            int swingStrength, double[] highs, double[] lows, int barCount,
            List<TrendState> statesSeen)
        {
            var detector = new PivotDetector(swingStrength);
            var sequence = new SwingSequence();
            var labelled = new List<LabelledSwing>();

            for (int i = 0; i < barCount; i++)
            {
                PivotResult result = detector.OnBar(i, TestBars.Make(highs[i], lows[i]));

                if (result.PivotHigh != null)
                    labelled.Add(Record(sequence, result.PivotHigh, i));
                if (result.PivotLow != null)
                    labelled.Add(Record(sequence, result.PivotLow, i));

                if (statesSeen != null)
                    statesSeen.Add(sequence.State);
            }

            return labelled;
        }

        private static LabelledSwing Record(SwingSequence sequence, Swing swing, int barIndex)
        {
            sequence.Add(swing);

            var record = new LabelledSwing();
            record.AddedAtBar = barIndex;
            record.Index = swing.Index;
            record.Price = swing.Price;
            record.Kind = swing.Kind;
            record.Label = swing.Label;
            record.ConfirmedAtIndex = swing.ConfirmedAtIndex;
            record.Instance = swing;
            return record;
        }

        [Fact]
        public void RealisticSeries_ReachesBothTrendDirections()
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            var states = new List<TrendState>();
            Run(3, highs, lows, BarCount, states);

            Assert.Contains(TrendState.Uptrend, states);
            Assert.Contains(TrendState.Downtrend, states);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void EveryLabelMatchesADirectPriceComparison(int swingStrength)
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<LabelledSwing> labelled = Run(swingStrength, highs, lows, BarCount, null);
            Assert.NotEmpty(labelled);

            bool haveHigh = false;
            bool haveLow = false;
            double previousHigh = 0.0;
            double previousLow = 0.0;

            for (int i = 0; i < labelled.Count; i++)
            {
                LabelledSwing swing = labelled[i];

                if (swing.Kind == SwingKind.High)
                {
                    TrendLabel expected = !haveHigh
                        ? TrendLabel.Undetermined
                        : Expected(swing.Price, previousHigh, TrendLabel.HH, TrendLabel.LH);

                    Assert.Equal(expected, swing.Label);
                    previousHigh = swing.Price;
                    haveHigh = true;
                }
                else
                {
                    TrendLabel expected = !haveLow
                        ? TrendLabel.Undetermined
                        : Expected(swing.Price, previousLow, TrendLabel.HL, TrendLabel.LL);

                    Assert.Equal(expected, swing.Label);
                    previousLow = swing.Price;
                    haveLow = true;
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void NoSwingIsLabelledBeforeItIsConfirmed(int swingStrength)
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<LabelledSwing> labelled = Run(swingStrength, highs, lows, BarCount, null);
            Assert.NotEmpty(labelled);

            foreach (LabelledSwing swing in labelled)
            {
                // Labelling happened on the bar that confirmed the pivot, and the
                // pivot itself sits SwingStrength bars further back.
                Assert.Equal(swing.ConfirmedAtIndex, swing.AddedAtBar);
                Assert.Equal(swing.AddedAtBar - swingStrength, swing.Index);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public void LabelsAssignedOverAPrefixNeverChangeLater(int swingStrength)
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<LabelledSwing> full = Run(swingStrength, highs, lows, BarCount, null);
            Assert.NotEmpty(full);

            // The whole pipeline, not just the detector, must leave the past alone.
            for (int n = 1; n <= BarCount; n++)
            {
                List<LabelledSwing> prefix = Run(swingStrength, highs, lows, n, null);

                var expected = new List<LabelledSwing>();
                for (int i = 0; i < full.Count; i++)
                {
                    if (full[i].AddedAtBar < n)
                        expected.Add(full[i]);
                }

                Assert.Equal(expected.Count, prefix.Count);

                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.Equal(expected[i].AddedAtBar, prefix[i].AddedAtBar);
                    Assert.Equal(expected[i].Index, prefix[i].Index);
                    Assert.Equal(expected[i].Price, prefix[i].Price);
                    Assert.Equal(expected[i].Kind, prefix[i].Kind);
                    Assert.Equal(expected[i].Label, prefix[i].Label);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void AnAssignedLabelIsNeverMutatedByALaterSwing(int swingStrength)
        {
            double[] highs;
            double[] lows;
            TestBars.BuildSeries(BarCount, out highs, out lows);

            List<LabelledSwing> labelled = Run(swingStrength, highs, lows, BarCount, null);
            Assert.NotEmpty(labelled);

            // SwingSequence holds live references in LastHigh and LastLow, and
            // Swing.Label is settable, so a later Add could retroactively rewrite
            // an earlier label. Comparing the live object against the value
            // recorded at assignment time is the only way to see that happen.
            foreach (LabelledSwing swing in labelled)
                Assert.Equal(swing.Label, swing.Instance.Label);
        }

        private static TrendLabel Expected(
            double current, double previous, TrendLabel higher, TrendLabel lower)
        {
            if (current > previous)
                return higher;
            if (current < previous)
                return lower;
            return TrendLabel.Undetermined;
        }
    }
}
