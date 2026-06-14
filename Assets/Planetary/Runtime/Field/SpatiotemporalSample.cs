using UnityEngine;
using Weather;

namespace Planetary.Field
{
    /// <summary>Blended or chart-specific sample from the canonical field.</summary>
    public struct SpatiotemporalSample
    {
        public ManifoldCellData cell;
        public Vector3 velocityWorld;
        public float surfaceFriction;
        public float surfaceTensionCoeff;
        public float transitionWeight;
        public SpatiotemporalChart dominantChart;

        public static SpatiotemporalSample FromCell(ManifoldCellData cell, Vector3 velocity, SpatiotemporalChart chart, float weight = 1f)
        {
            return new SpatiotemporalSample
            {
                cell = cell,
                velocityWorld = velocity,
                surfaceFriction = cell.surfaceFriction > 1e-6f ? cell.surfaceFriction : 0.7f,
                surfaceTensionCoeff = cell.surfaceTensionCoeff,
                transitionWeight = weight,
                dominantChart = chart
            };
        }
    }
}
