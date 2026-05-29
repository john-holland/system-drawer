using UnityEngine;

namespace SdfMax
{
    public sealed class IntegralConvexTreeNode
    {
        public Bounds LeafBounds;
        public int DominantChildIndex = -1;
        public float IntegratedMeasure;
    }
}
