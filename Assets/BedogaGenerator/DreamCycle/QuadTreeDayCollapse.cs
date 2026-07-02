using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BedogaGenerator.DreamCycle
{
    /// <summary>Collapses day spatial generators into a stable dayCollapseSeed digest.</summary>
    public static class QuadTreeDayCollapse
    {
        public struct CollapseResult
        {
            public int dayCollapseSeed;
            public string digestHex;
            public int generatorCount;
        }

        public static CollapseResult Collapse(IEnumerable<SpatialGenerator> generators)
        {
            var sb = new StringBuilder();
            int count = 0;
            if (generators != null)
            {
                foreach (var g in generators)
                {
                    if (g == null)
                        continue;
                    count++;
                    sb.Append(g.name).Append(':').Append(g.seed).Append(':').Append(g.mode).Append('|');
                    var quad = g.GetComponent<SGQuadTreeSolver>();
                    if (quad != null)
                    {
                        sb.Append("md=").Append(quad.maxDepth).Append("mo=").Append(quad.maxObjectsPerNode).Append('|');
                    }
                }
            }
            string digest = sb.ToString();
            int seed = StableHash(digest);
            string hex = seed.ToString("x8");
            return new CollapseResult
            {
                dayCollapseSeed = seed,
                digestHex = hex,
                generatorCount = count
            };
        }

        public static CollapseResult CollapseFromOrchestrator(SpatialGenerator4DOrchestrator orchestrator)
        {
            var list = new List<SpatialGenerator>();
            if (orchestrator?.spatialGenerators != null)
            {
                foreach (var gen in orchestrator.spatialGenerators)
                {
                    if (gen is SpatialGenerator sg)
                        list.Add(sg);
                }
            }
            return Collapse(list);
        }

        static int StableHash(string s)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < s.Length; i++)
                    hash = hash * 31 + s[i];
                return hash;
            }
        }
    }
}
