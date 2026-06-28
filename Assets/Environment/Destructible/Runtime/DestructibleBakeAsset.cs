using System;
using System.Collections.Generic;
using UnityEngine;

namespace DestructibleEnvironment
{
    [Serializable]
    public struct DestructiblePieceRecord
    {
        public int pieceId;
        public int parentPieceId;
        public int treeDepth;
        public float normalizedVolume;
        public Mesh pieceMesh;
        public Bounds localBounds;
        public Vector3 localCentroid;
        public float massEstimate;
        public int[] neighborPieceIds;
        public int[] supportPieceIds;
        public float groundRayMaxDistance;
    }

    [CreateAssetMenu(fileName = "DestructibleBake", menuName = "Environment/Destructible Bake")]
    public class DestructibleBakeAsset : ScriptableObject
    {
        public int sourceMeshInstanceId;
        public string sourceMeshName;
        public Vector3 sourceLossyScale;

        public int maxDepth = ConvexMeshTreeCacheBuilder.DefaultMaxDepth;
        public float minLeafExtent = ConvexMeshTreeCacheBuilder.DefaultMinExtent;
        public int maxTrianglesPerLeaf = ConvexMeshTreeCacheBuilder.DefaultMaxTrianglesPerLeaf;
        public float minRubbleVolume = 0.001f;

        public Bounds rootLocalBounds;
        public float rootVolume;

        public List<DestructiblePieceRecord> pieces = new List<DestructiblePieceRecord>();
        public int[] fallOrder = Array.Empty<int>();
        public int poolSlotCount;
        public GameObject behaviorTreePrefab;

        public int PieceCount => pieces != null ? pieces.Count : 0;

        public bool TryGetPiece(int pieceId, out DestructiblePieceRecord record)
        {
            if (pieces == null)
            {
                record = default;
                return false;
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].pieceId == pieceId)
                {
                    record = pieces[i];
                    return true;
                }
            }

            record = default;
            return false;
        }
    }
}
