using System.Collections.Generic;
using UnityEngine;

namespace DestructibleEnvironment
{
    public static class DestructiblePreBakePipeline
    {
        public struct SourceMeshEntry
        {
            public Mesh mesh;
            public Matrix4x4 localToWorld;
            public Matrix4x4 worldToLocal;
        }

        public static void PopulateBakeAsset(
            DestructibleBakeAsset asset,
            IReadOnlyList<SourceMeshEntry> sources,
            DestructibleMaterialProfile profile,
            Vector3 gravityDir,
            int maxDepth,
            float minLeafExtent,
            int maxTrianglesPerLeaf,
            float minRubbleVolume)
        {
            if (asset == null || sources == null || sources.Count == 0)
                return;

            asset.pieces.Clear();
            asset.maxDepth = maxDepth;
            asset.minLeafExtent = minLeafExtent;
            asset.maxTrianglesPerLeaf = maxTrianglesPerLeaf;
            asset.minRubbleVolume = minRubbleVolume;

            var combinedBounds = new Bounds();
            bool firstBounds = true;
            float totalRootVolume = 0f;

            for (int s = 0; s < sources.Count; s++)
            {
                SourceMeshEntry entry = sources[s];
                if (entry.mesh == null)
                    continue;

                if (s == 0)
                {
                    asset.sourceMeshInstanceId = entry.mesh.GetInstanceID();
                    asset.sourceMeshName = entry.mesh.name;
                }

                var tree = DestructibleConvexTreeBuilder.BuildFromMesh(
                    entry.mesh,
                    entry.localToWorld,
                    maxDepth,
                    minLeafExtent,
                    maxTrianglesPerLeaf);

                totalRootVolume += tree.RootVolume;

                if (firstBounds)
                {
                    combinedBounds = TransformBounds(tree.RootBounds, entry.worldToLocal);
                    firstBounds = false;
                }
                else
                    combinedBounds.Encapsulate(TransformBounds(tree.RootBounds, entry.worldToLocal));

                int pieceBase = asset.pieces.Count;
                for (int l = 0; l < tree.Leaves.Count; l++)
                {
                    DestructibleTreeLeaf leaf = tree.Leaves[l];
                    float leafVolume = Volume(leaf.WorldBounds);
                    if (leafVolume < minRubbleVolume)
                        continue;

                    Mesh pieceMesh = DestructibleMeshExtractor.ExtractSubmesh(
                        entry.mesh,
                        entry.localToWorld,
                        entry.worldToLocal,
                        leaf.TriangleIndices,
                        $"DestructiblePiece_{pieceBase + l}");

                    if (pieceMesh == null)
                        continue;

                    float density = profile != null ? profile.densityKgPerM3 : 2400f;
                    Bounds localBounds = pieceMesh.bounds;
                    int pieceId = asset.pieces.Count;
                    int parentPieceId = leaf.ParentLeafIndex >= 0 ? pieceBase + leaf.ParentLeafIndex : -1;

                    var record = new DestructiblePieceRecord
                    {
                        pieceId = pieceId,
                        parentPieceId = parentPieceId >= 0 && parentPieceId < pieceId ? parentPieceId : -1,
                        treeDepth = leaf.Depth,
                        normalizedVolume = leafVolume / Mathf.Max(tree.RootVolume, 1e-8f),
                        pieceMesh = pieceMesh,
                        localBounds = localBounds,
                        localCentroid = localBounds.center,
                        massEstimate = leafVolume * density,
                        groundRayMaxDistance = EstimateGroundRayDistance(localBounds, gravityDir)
                    };
                    asset.pieces.Add(record);
                }
            }

            asset.rootLocalBounds = combinedBounds;
            asset.rootVolume = Mathf.Max(totalRootVolume, 1e-8f);

            for (int i = 0; i < asset.pieces.Count; i++)
            {
                DestructiblePieceRecord p = asset.pieces[i];
                p.normalizedVolume = VolumeFromBounds(p.localBounds) / asset.rootVolume;
                asset.pieces[i] = p;
            }

            DestructiblePieceGraph.BuildAdjacency(asset.pieces, gravityDir);
            asset.fallOrder = DestructiblePieceGraph.ComputeFallOrder(asset.pieces);
            asset.poolSlotCount = asset.pieces.Count;
        }

        static Bounds TransformBounds(Bounds worldBounds, Matrix4x4 worldToLocal)
        {
            Vector3 c = worldBounds.center;
            Vector3 e = worldBounds.extents;
            var corners = new Vector3[8];
            int n = 0;
            for (int ix = -1; ix <= 1; ix += 2)
            for (int iy = -1; iy <= 1; iy += 2)
            for (int iz = -1; iz <= 1; iz += 2)
                corners[n++] = worldToLocal.MultiplyPoint3x4(c + Vector3.Scale(e, new Vector3(ix, iy, iz)));

            var local = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                local.Encapsulate(corners[i]);
            return local;
        }

        static float Volume(Bounds b) => b.size.x * b.size.y * b.size.z;

        static float VolumeFromBounds(Bounds b) => Volume(b);

        static float EstimateGroundRayDistance(Bounds localBounds, Vector3 gravityDir)
        {
            Vector3 down = gravityDir.sqrMagnitude > 1e-6f ? gravityDir.normalized : Vector3.down;
            float extentAlongDown = Mathf.Abs(Vector3.Dot(localBounds.extents, down));
            return localBounds.size.magnitude + extentAlongDown + 2f;
        }
    }
}
