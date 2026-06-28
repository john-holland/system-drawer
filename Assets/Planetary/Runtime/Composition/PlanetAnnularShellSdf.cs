using UnityEngine;

namespace Planetary.Composition
{
    /// <summary>Signed distance for a spherical shell band (negative inside the annulus material).</summary>
    public static class PlanetAnnularShellSdf
    {
        public static float SignedDistance(float distFromCenter, float innerRadius, float outerRadius)
        {
            return Mathf.Max(distFromCenter - outerRadius, innerRadius - distFromCenter);
        }

        public static bool IsInsideBand(float distFromCenter, float innerRadius, float outerRadius) =>
            SignedDistance(distFromCenter, innerRadius, outerRadius) <= 0f;
    }
}
