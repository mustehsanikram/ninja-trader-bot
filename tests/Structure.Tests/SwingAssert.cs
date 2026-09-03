using System.Collections.Generic;
using PkStructure;
using Xunit;

namespace PkStructure.Tests
{
    /// <summary>
    /// Shared comparisons for swings and confirmation streams. Kept apart from
    /// TestBars so the data builders stay free of a test framework reference.
    /// </summary>
    internal static class SwingAssert
    {
        /// <summary>Null expected means null actual. Compares every field a
        /// caller can observe.</summary>
        public static void SameSwing(Swing expected, Swing actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.NotNull(actual);
            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Price, actual.Price);
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.ConfirmedAtIndex, actual.ConfirmedAtIndex);
        }

        public static void SameConfirmations(
            List<Confirmation> expected, List<Confirmation> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].BarIndex, actual[i].BarIndex);
                SameSwing(expected[i].PivotHigh, actual[i].PivotHigh);
                SameSwing(expected[i].PivotLow, actual[i].PivotLow);
            }
        }
    }
}
