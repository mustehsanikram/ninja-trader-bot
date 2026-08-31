namespace PkStructure
{
    /// <summary>What a single bar confirmed. Either side may be null.</summary>
    public struct PivotResult
    {
        private readonly Swing _pivotHigh;
        private readonly Swing _pivotLow;

        public PivotResult(Swing pivotHigh, Swing pivotLow)
        {
            _pivotHigh = pivotHigh;
            _pivotLow = pivotLow;
        }

        public Swing PivotHigh { get { return _pivotHigh; } }

        public Swing PivotLow { get { return _pivotLow; } }

        public bool HasAny { get { return _pivotHigh != null || _pivotLow != null; } }
    }
}
