using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Projects galactic registry bodies to observer-local star directions.</summary>
    public static class GalacticNightSkyStarCatalog
    {
        public struct StarPoint
        {
            public Vector3 direction;
            public float magnitude;
            public Color color;
            public string bodyId;
        }

        public static List<StarPoint> BuildVisibleStars(
            Vector3 observerWorld,
            IEnumerable<GalacticBodyRecord> bodies,
            Transform galacticOrigin,
            float magnitudeCutoff = 6f)
        {
            var result = new List<StarPoint>();
            if (bodies == null)
                return result;
            foreach (var b in bodies)
            {
                if (b.kind == GalacticBodyKind.Star && b.bodyId == "sol")
                    continue;
                Vector3 world = GalacticFrame.GalacticToWorld(b.galacticPosition, galacticOrigin);
                Vector3 dir = (world - observerWorld).normalized;
                float dist = Vector3.Distance(observerWorld, world);
                float mag = Mathf.Log10(Mathf.Max(dist, 1f) / b.radiusM) * 2.5f + 1f;
                if (mag > magnitudeCutoff)
                    continue;
                result.Add(new StarPoint
                {
                    direction = dir,
                    magnitude = mag,
                    color = b.kind == GalacticBodyKind.Star ? Color.white : new Color(0.7f, 0.7f, 0.8f),
                    bodyId = b.bodyId
                });
            }
            return result;
        }
    }
}
