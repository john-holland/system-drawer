using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Voxel
{
    public enum LoopEdgePermeability
    {
        Blocked = 0,
        Permeable = 1
    }

    public sealed class LoopEdgeMap
    {
        public struct Edge
        {
            public int CellA;
            public int CellB;
            public LoopEdgePermeability Permeability;
        }

        readonly List<Edge> _edges = new List<Edge>();

        public IReadOnlyList<Edge> Edges => _edges;

        public void AddEdge(int cellA, int cellB, LoopEdgePermeability permeability)
        {
            _edges.Add(new Edge { CellA = cellA, CellB = cellB, Permeability = permeability });
        }

        public bool TryDetectBreach(int cellA, int cellB, float stress, float surfaceTensionCoeff)
        {
            float threshold = Mathf.Lerp(float.MaxValue, 0.1f, Mathf.Clamp01(surfaceTensionCoeff));
            if (stress < threshold)
                return false;
            for (int i = 0; i < _edges.Count; i++)
            {
                var e = _edges[i];
                if ((e.CellA == cellA && e.CellB == cellB) || (e.CellA == cellB && e.CellB == cellA))
                    return e.Permeability == LoopEdgePermeability.Permeable && stress >= threshold;
            }
            return stress >= threshold;
        }

        public void FloodLiquidThroughput(DualEncodedVoxelField field, int startCell, HashSet<int> reached)
        {
            reached.Clear();
            var q = new Queue<int>();
            q.Enqueue(startCell);
            reached.Add(startCell);
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                for (int i = 0; i < _edges.Count; i++)
                {
                    var e = _edges[i];
                    if (e.Permeability != LoopEdgePermeability.Permeable)
                        continue;
                    int next = e.CellA == c ? e.CellB : e.CellB == c ? e.CellA : -1;
                    if (next < 0 || reached.Contains(next))
                        continue;
                    reached.Add(next);
                    q.Enqueue(next);
                }
            }
        }
    }
}
