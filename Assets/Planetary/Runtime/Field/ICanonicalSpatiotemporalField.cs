using UnityEngine;

namespace Planetary.Field
{
    /// <summary>Canonical spatiotemporal field with chart pullbacks and blended sampling.</summary>
    public interface ICanonicalSpatiotemporalField
    {
        bool TrySample(Vector3 world, float narrativeTime, SpatiotemporalChart requestedChart, out SpatiotemporalSample sample);
        bool TrySampleBlended(Vector3 world, float narrativeTime, out SpatiotemporalSample sample);
    }
}
