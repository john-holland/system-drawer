using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    [CreateAssetMenu(fileName = "PlanetaryPlanarFeatureStack", menuName = "Planetary/Planar Feature Stack")]
    public sealed class PlanetaryPlanarFeatureStack : ScriptableObject
    {
        public List<PlanetaryPlanarFeature> features = new List<PlanetaryPlanarFeature>();
    }
}
