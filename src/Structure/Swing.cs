using System;

namespace PkStructure
{
    public sealed class Swing
    {
        public Swing(int index, double price, SwingKind kind, int swingStrength)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (swingStrength < 1)
                throw new ArgumentOutOfRangeException("swingStrength");

            Index = index;
            Price = price;
            Kind = kind;
            Label = TrendLabel.Undetermined;

            // A pivot cannot be known until SwingStrength bars have formed to its
            // right. Nothing may be drawn or acted on before this bar.
            ConfirmedAtIndex = index + swingStrength;
        }

        public int Index { get; private set; }
        public double Price { get; private set; }
        public SwingKind Kind { get; private set; }
        public TrendLabel Label { get; set; }
        public int ConfirmedAtIndex { get; private set; }
    }
}
