using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    public sealed class PlanetRenderer : MonoBehaviour
    {
        readonly List<MeshFilter> _chunkFilters = new List<MeshFilter>();
        readonly Dictionary<(PlanetFaceId, int, int), MeshFilter> _chunkMap = new Dictionary<(PlanetFaceId, int, int), MeshFilter>();
        public Material chunkMaterial;

        public void SetChunks(PlanetMeshChunk[] chunks, Transform parent)
        {
            Clear();
            if (chunks == null)
                return;
            for (int i = 0; i < chunks.Length; i++)
            {
                var go = new GameObject($"Chunk_{chunks[i].Face}_{chunks[i].ChunkX}_{chunks[i].ChunkY}");
                go.transform.SetParent(parent, false);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = chunks[i].Mesh;
                if (chunkMaterial != null)
                    mr.sharedMaterial = chunkMaterial;
                _chunkFilters.Add(mf);
                _chunkMap[(chunks[i].Face, chunks[i].ChunkX, chunks[i].ChunkY)] = mf;
            }
        }

        public bool TrySetChunkMesh(PlanetFaceId face, int chunkX, int chunkY, Mesh mesh)
        {
            if (!_chunkMap.TryGetValue((face, chunkX, chunkY), out var mf) || mf == null)
                return false;
            mf.sharedMesh = mesh;
            return true;
        }

        public void Clear()
        {
            for (int i = _chunkFilters.Count - 1; i >= 0; i--)
            {
                if (_chunkFilters[i] != null)
                    DestroyImmediate(_chunkFilters[i].gameObject);
            }
            _chunkFilters.Clear();
            _chunkMap.Clear();
        }
    }
}
