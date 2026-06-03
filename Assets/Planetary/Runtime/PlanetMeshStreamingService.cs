using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Planetary
{
    public sealed class PlanetMeshStreamingService : MonoBehaviour
    {
        [Tooltip("Continuum base URL e.g. http://localhost:5050")]
        public string continuumBaseUrl = "http://localhost:5050";
        public string planetId = "default";
        public int maxCacheEntries = 64;

        readonly Dictionary<string, float> _heightCache = new Dictionary<string, float>();
        readonly Dictionary<string, Mesh> _chunkMeshes = new Dictionary<string, Mesh>();
        readonly Queue<string> _lru = new Queue<string>();

        public event Action<PlanetFaceId, int, int, int, Mesh> OnChunkLoaded;

        public float SampleCachedHeight(float latDeg, float lonDeg)
        {
            string key = $"{latDeg:F2}_{lonDeg:F2}";
            return _heightCache.TryGetValue(key, out float h) ? h : 0f;
        }

        public void CacheHeight(float latDeg, float lonDeg, float height)
        {
            string key = $"{latDeg:F2}_{lonDeg:F2}";
            if (!_heightCache.ContainsKey(key))
            {
                _lru.Enqueue(key);
                while (_lru.Count > maxCacheEntries)
                    _heightCache.Remove(_lru.Dequeue());
            }
            _heightCache[key] = height;
        }

        static string ChunkKey(PlanetFaceId face, int lod, int x, int y) => $"{(int)face}_{lod}_{x}_{y}";

        public bool TryGetLoadedChunk(PlanetFaceId face, int lod, int chunkX, int chunkY, out Mesh mesh)
        {
            return _chunkMeshes.TryGetValue(ChunkKey(face, lod, chunkX, chunkY), out mesh);
        }

        public float GetCoverageFraction(float latDeg, float lonDeg, int chunksPerFace)
        {
            int loaded = 0;
            int total = chunksPerFace * chunksPerFace;
            for (int cx = 0; cx < chunksPerFace; cx++)
            for (int cy = 0; cy < chunksPerFace; cy++)
            {
                if (_chunkMeshes.ContainsKey(ChunkKey(PlanetFaceId.PosX, 0, cx, cy)))
                    loaded++;
            }
            _ = latDeg;
            _ = lonDeg;
            return total > 0 ? (float)loaded / total : 0f;
        }

        public void RequestTile(PlanetFaceId face, int lod, int chunkX, int chunkY, Action<byte[]> onDone)
        {
            string url = $"{continuumBaseUrl.TrimEnd('/')}/api/planet/tiles?planet_id={planetId}&face={(int)face}&lod={lod}&x={chunkX}&y={chunkY}";
            StartCoroutine(GetTile(url, face, lod, chunkX, chunkY, onDone));
        }

        public void RequestTilesAroundPlayer(PlanetBody body, int lod, int radiusChunks)
        {
            if (body == null)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;
            var sc = SphericalCoordinates.FromWorldPosition(
                cam.transform.position, body.PlanetCenter, body.StablePoleAxis, body.PrimeMeridianOffsetDeg);
            int cx = Mathf.Clamp(Mathf.FloorToInt((sc.LongitudeDeg + 180f) / 360f * body.chunksPerFace), 0, body.chunksPerFace - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt((sc.LatitudeDeg + 90f) / 180f * body.chunksPerFace), 0, body.chunksPerFace - 1);
            for (int f = 0; f < 6; f++)
            for (int dx = -radiusChunks; dx <= radiusChunks; dx++)
            for (int dy = -radiusChunks; dy <= radiusChunks; dy++)
            {
                int x = Mathf.Clamp(cx + dx, 0, body.chunksPerFace - 1);
                int y = Mathf.Clamp(cy + dy, 0, body.chunksPerFace - 1);
                var face = (PlanetFaceId)f;
                RequestTile(face, lod, x, y, data => OnTileBytes(face, lod, x, y, data, body));
            }
        }

        void OnTileBytes(PlanetFaceId face, int lod, int x, int y, byte[] data, PlanetBody body)
        {
            if (data == null || data.Length < 4 || body == null)
                return;
            var mesh = DecodeTileMesh(data, body);
            if (mesh == null)
                return;
            string key = ChunkKey(face, lod, x, y);
            _chunkMeshes[key] = mesh;
            OnChunkLoaded?.Invoke(face, lod, x, y, mesh);
            if (body.planetRenderer != null)
                body.planetRenderer.TrySetChunkMesh(face, x, y, mesh);
        }

        static Mesh DecodeTileMesh(byte[] data, PlanetBody body)
        {
            int res = Mathf.Clamp(BitConverter.ToInt32(data, 0), 4, 128);
            if (data.Length < 4 + res * res * 4)
                return null;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float r = body.PlanetRadius;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                int o = 4 + (y * res + x) * 4;
                float h = BitConverter.ToSingle(data, o);
                float u = x / (float)(res - 1);
                float v = y / (float)(res - 1);
                Vector3 cube = PlanetCubeSphere6Face.FaceUvToCube(PlanetFaceId.PosX, u, v);
                verts.Add(PlanetCubeSphere6Face.CubeToSphere(cube, r + h));
            }
            for (int y = 0; y < res - 1; y++)
            for (int x = 0; x < res - 1; x++)
            {
                int i0 = y * res + x;
                tris.Add(i0); tris.Add(i0 + res); tris.Add(i0 + 1);
                tris.Add(i0 + 1); tris.Add(i0 + res); tris.Add(i0 + res + 1);
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        System.Collections.IEnumerator GetTile(string url, PlanetFaceId face, int lod, int x, int y, Action<byte[]> onDone)
        {
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                onDone?.Invoke(req.downloadHandler.data);
            else
                onDone?.Invoke(null);
        }
    }
}
