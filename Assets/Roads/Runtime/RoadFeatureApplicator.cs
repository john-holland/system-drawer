using Roads.Features;
using UnityEngine;

namespace Roads
{
    /// <summary>Applies child road features to baked samples before mesh generation.</summary>
    public static class RoadFeatureApplicator
    {
        public static void ApplyFeatures(Transform roadRoot, RoadSplineSample[] samples)
        {
            if (roadRoot == null || samples == null || samples.Length == 0)
                return;
            var features = roadRoot.GetComponentsInChildren<RoadFeatureBase>(true);
            foreach (var feature in features)
                feature.ApplyToSamples(samples);
        }
    }
}
