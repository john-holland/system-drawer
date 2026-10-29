namespace SdfMax
{
    /// <summary>
    /// Counts bone <c>hasChanged</c> signals that did not change the matrix (a flipped flag)
    /// versus signals that actually moved. Drained by the pathing frame trace.
    /// </summary>
    public static class SdfMaxShapeSyncStats
    {
        public static int FlippedHasChanged;
        public static int ShapeChanges;
    }
}
