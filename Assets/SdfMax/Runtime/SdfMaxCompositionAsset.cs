using System.Collections.Generic;
using UnityEngine;

namespace SdfMax
{
    [CreateAssetMenu(fileName = "SdfMaxComposition", menuName = "SDF Max/Composition")]
    public sealed class SdfMaxCompositionAsset : ScriptableObject
    {
        [Tooltip("Root node is the last entry when using list builder, or index 0 when single root.")]
        public int rootNodeIndex = -1;

        public List<SdfMaxNode> nodes = new List<SdfMaxNode>();

        public int ResolveRootIndex()
        {
            if (rootNodeIndex >= 0 && rootNodeIndex < nodes.Count)
                return rootNodeIndex;
            return nodes.Count > 0 ? nodes.Count - 1 : -1;
        }
    }
}
