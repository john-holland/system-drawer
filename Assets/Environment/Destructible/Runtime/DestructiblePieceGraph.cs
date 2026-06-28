using System;
using System.Collections.Generic;
using UnityEngine;

namespace DestructibleEnvironment
{
    public static class DestructiblePieceGraph
    {
        public static void BuildAdjacency(
            IList<DestructiblePieceRecord> pieces,
            Vector3 gravityDir,
            float horizontalOverlapTolerance = 0.05f,
            float neighborDistanceTolerance = 0.15f)
        {
            if (pieces == null || pieces.Count == 0)
                return;

            Vector3 down = gravityDir.sqrMagnitude > 1e-6f ? gravityDir.normalized : Vector3.down;
            Vector3 up = -down;

            for (int i = 0; i < pieces.Count; i++)
            {
                var neighbors = new List<int>();
                var supports = new List<int>();
                DestructiblePieceRecord a = pieces[i];

                for (int j = 0; j < pieces.Count; j++)
                {
                    if (i == j)
                        continue;

                    DestructiblePieceRecord b = pieces[j];
                    float dist = Vector3.Distance(a.localCentroid, b.localCentroid);
                    if (dist <= neighborDistanceTolerance + a.localBounds.extents.magnitude + b.localBounds.extents.magnitude)
                        neighbors.Add(b.pieceId);

                    if (IsSupportRelation(a, b, up, horizontalOverlapTolerance))
                        supports.Add(b.pieceId);
                }

                a.neighborPieceIds = neighbors.ToArray();
                a.supportPieceIds = supports.ToArray();
                pieces[i] = a;
            }
        }

        static bool IsSupportRelation(
            DestructiblePieceRecord lower,
            DestructiblePieceRecord upper,
            Vector3 up,
            float horizontalTolerance)
        {
            float heightDelta = Vector3.Dot(upper.localCentroid - lower.localCentroid, up);
            if (heightDelta <= 0.01f)
                return false;

            Vector3 lowerFlat = ProjectOnPlane(lower.localCentroid, up);
            Vector3 upperFlat = ProjectOnPlane(upper.localCentroid, up);
            if (Vector3.Distance(lowerFlat, upperFlat) > horizontalTolerance + HorizontalExtent(lower.localBounds, up) + HorizontalExtent(upper.localBounds, up))
                return false;

            return lower.localBounds.Intersects(ExpandOnPlane(upper.localBounds, up, horizontalTolerance));
        }

        static Vector3 ProjectOnPlane(Vector3 v, Vector3 planeNormal)
        {
            return v - planeNormal * Vector3.Dot(v, planeNormal);
        }

        static float HorizontalExtent(Bounds b, Vector3 up)
        {
            Vector3 e = b.extents;
            Vector3 ax = Vector3.Cross(up, Vector3.forward);
            if (ax.sqrMagnitude < 1e-4f)
                ax = Vector3.Cross(up, Vector3.right);
            ax.Normalize();
            Vector3 az = Vector3.Cross(up, ax);
            return Mathf.Max(Mathf.Abs(Vector3.Dot(e, ax)), Mathf.Abs(Vector3.Dot(e, az)));
        }

        static Bounds ExpandOnPlane(Bounds b, Vector3 up, float pad)
        {
            Vector3 size = b.size;
            Vector3 ax = Vector3.Cross(up, Vector3.forward);
            if (ax.sqrMagnitude < 1e-4f)
                ax = Vector3.Cross(up, Vector3.right);
            ax.Normalize();
            Vector3 az = Vector3.Cross(up, ax);
            size += (Mathf.Abs(Vector3.Dot(ax, Vector3.one)) + pad) * ax;
            size += (Mathf.Abs(Vector3.Dot(az, Vector3.one)) + pad) * az;
            return new Bounds(b.center, size);
        }

        public static int[] ComputeFallOrder(IList<DestructiblePieceRecord> pieces)
        {
            if (pieces == null || pieces.Count == 0)
                return Array.Empty<int>();

            var idToIndex = new Dictionary<int, int>();
            for (int i = 0; i < pieces.Count; i++)
                idToIndex[pieces[i].pieceId] = i;

            var inDegree = new int[pieces.Count];
            var dependents = new List<int>[pieces.Count];
            for (int i = 0; i < pieces.Count; i++)
                dependents[i] = new List<int>();

            for (int i = 0; i < pieces.Count; i++)
            {
                int[] supports = pieces[i].supportPieceIds;
                if (supports == null)
                    continue;

                for (int s = 0; s < supports.Length; s++)
                {
                    if (!idToIndex.TryGetValue(supports[s], out int supportIdx))
                        continue;
                    dependents[supportIdx].Add(i);
                    inDegree[i]++;
                }
            }

            var queue = new List<int>();
            for (int i = 0; i < pieces.Count; i++)
            {
                if (inDegree[i] == 0)
                    queue.Add(i);
            }

            queue.Sort((a, b) => HeightCompare(pieces, a, b));

            var order = new List<int>(pieces.Count);
            while (queue.Count > 0)
            {
                int idx = queue[0];
                queue.RemoveAt(0);
                order.Add(pieces[idx].pieceId);

                for (int d = 0; d < dependents[idx].Count; d++)
                {
                    int dep = dependents[idx][d];
                    inDegree[dep]--;
                    if (inDegree[dep] == 0)
                    {
                        int insert = queue.Count;
                        for (int q = 0; q < queue.Count; q++)
                        {
                            if (HeightCompare(pieces, dep, queue[q]) > 0)
                            {
                                insert = q;
                                break;
                            }
                        }
                        queue.Insert(insert, dep);
                    }
                }
            }

            if (order.Count < pieces.Count)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (!order.Contains(pieces[i].pieceId))
                        order.Add(pieces[i].pieceId);
                }
            }

            return order.ToArray();
        }

        static int HeightCompare(IList<DestructiblePieceRecord> pieces, int a, int b)
        {
            return pieces[b].localCentroid.y.CompareTo(pieces[a].localCentroid.y);
        }
    }
}
