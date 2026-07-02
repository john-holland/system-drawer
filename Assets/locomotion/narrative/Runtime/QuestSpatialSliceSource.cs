using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>Abstract slice provider for quest orthographic maps (implemented by Bedoga Spatial4D bridge).</summary>
    public abstract class QuestSpatialSliceSource : MonoBehaviour
    {
        public abstract bool TryGetSliceAtT(
            float t,
            out Bounds bounds,
            out int resX,
            out int resY,
            out int resZ,
            out float[] occupancy,
            out float[] causal);

        public virtual float NarrativeTMin => 0f;
        public virtual float NarrativeTMax => 3600f;
    }
}
