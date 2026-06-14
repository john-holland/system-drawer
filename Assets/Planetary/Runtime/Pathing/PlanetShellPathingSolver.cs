using System.Collections.Generic;
using Planetary.Bridges;
using UnityEngine;

namespace Planetary.Pathing{
    /// <summary>
    /// Great-circle pathing on a planet shell when <see cref="PlanetShellManifoldGrid"/> is present.
    /// </summary>
    public sealed class PlanetShellPathingSolver : IPhysicalPathingSolver
    {
        public PhysicalPathingMedium Medium => PhysicalPathingMedium.Space;
        public PlanetBody Planet;
        public PlanetShellManifoldGrid ShellGrid;

        public bool TryFindPath(
            HierarchicalPathingSolver context,
            Vector3 startWorld,
            Vector3 goalWorld,
            bool returnBestEffortPathWhenNoPath,
            out List<Vector3> path)
        {
            // todo: review: this is as the crow flies, but also a decent ark tool, however
            // it may not be what we want later, so just a todo: review to keep track
            path = new List<Vector3>();
            if (Planet == null)
                return false;

            Vector3 center = Planet.PlanetCenter;
            Vector3 startDir = (startWorld - center).normalized;
            Vector3 goalDir = (goalWorld - center).normalized;
            float radius = Vector3.Distance(startWorld, center);
            if (radius < Planet.PlanetRadius * 0.5f)
                radius = Planet.PlanetRadius;

            int steps = 24;
            path.Add(startWorld);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 dir = Vector3.Slerp(startDir, goalDir, t);
                path.Add(center + dir * radius);
            }

            return path.Count > 1; // also always returns true
        }
    }
}
