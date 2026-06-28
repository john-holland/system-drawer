using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    public sealed class PlanetMeshChunk
    {
        public PlanetFaceId Face;
        public int ChunkX;
        public int ChunkY;
        public Mesh Mesh;
    }

    public static class PlanetMeshBuilder
    {
        public static PlanetMeshChunk[] BuildChunks(
            float radius,
            int resolution,
            int chunksPerFace,
            System.Func<float, float, float> heightAtLatLon,
            Vector3 planetCenterLocal = default,
            Vector3 stablePoleAxis = default)
        {
            if (stablePoleAxis.sqrMagnitude < 1e-6f)
                stablePoleAxis = Vector3.up;
            var result = new List<PlanetMeshChunk>();
            var faceVerts = new List<Vector3>[6];
            var faceTris = new List<int>[6];
            for (int f = 0; f < 6; f++)
            {
                faceVerts[f] = new List<Vector3>();
                faceTris[f] = new List<int>();
                BuildFace((PlanetFaceId)f, radius, resolution, heightAtLatLon, faceVerts[f], faceTris[f]);
            }
            VolleyballCornerStitcher.WeldCorners(faceVerts, faceTris);

            for (int f = 0; f < 6; f++)
            {
                for (int cx = 0; cx < chunksPerFace; cx++)
                for (int cy = 0; cy < chunksPerFace; cy++)
                {
                    var mesh = BuildChunkMesh(
                        (PlanetFaceId)f,
                        radius,
                        resolution,
                        chunksPerFace,
                        cx,
                        cy,
                        heightAtLatLon,
                        planetCenterLocal,
                        stablePoleAxis);
                    result.Add(new PlanetMeshChunk { Face = (PlanetFaceId)f, ChunkX = cx, ChunkY = cy, Mesh = mesh });
                }
            }
            return result.ToArray();
        }

        static void BuildFace(
            PlanetFaceId face,
            float radius,
            int res,
            System.Func<float, float, float> heightAtLatLon,
            List<Vector3> verts,
            List<int> tris)
        {
            for (int y = 0; y <= res; y++)
            for (int x = 0; x <= res; x++)
            {
                float u = x / (float)res;
                float v = y / (float)res;
                Vector3 cube = PlanetCubeSphere6Face.FaceUvToCube(face, u, v);
                Vector3 sphere = PlanetCubeSphere6Face.CubeToSphere(cube, radius);
                var sc = SphericalCoordinates.FromWorldPosition(sphere, Vector3.zero, Vector3.up, 0f);
                sphere = sphere.normalized * (radius + (heightAtLatLon?.Invoke(sc.LatitudeDeg, sc.LongitudeDeg) ?? 0f));
                verts.Add(sphere);
            }
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                int i0 = y * (res + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (res + 1);
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }

        static Mesh BuildChunkMesh(
            PlanetFaceId face,
            float radius,
            int res,
            int chunksPerFace,
            int cx,
            int cy,
            System.Func<float, float, float> heightAtLatLon,
            Vector3 planetCenterLocal,
            Vector3 stablePoleAxis)
        {
            int sub = Mathf.Max(2, res / chunksPerFace);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float u0 = cx / (float)chunksPerFace;
            float u1 = (cx + 1) / (float)chunksPerFace;
            float v0 = cy / (float)chunksPerFace;
            float v1 = (cy + 1) / (float)chunksPerFace;
            for (int y = 0; y <= sub; y++)
            for (int x = 0; x <= sub; x++)
            {
                float u = Mathf.Lerp(u0, u1, x / (float)sub);
                float v = Mathf.Lerp(v0, v1, y / (float)sub);
                Vector3 cube = PlanetCubeSphere6Face.FaceUvToCube(face, u, v);
                Vector3 sphere = PlanetCubeSphere6Face.CubeToSphere(cube, radius);
                var sc = SphericalCoordinates.FromWorldPosition(sphere, Vector3.zero, Vector3.up, 0f);
                sphere = sphere.normalized * (radius + (heightAtLatLon?.Invoke(sc.LatitudeDeg, sc.LongitudeDeg) ?? 0f));
                verts.Add(sphere);
            }
            for (int y = 0; y < sub; y++)
            for (int x = 0; x < sub; x++)
            {
                int i0 = y * (sub + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (sub + 1);
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
            var mesh = new Mesh { name = $"Planet_{face}_{cx}_{cy}" };
            ApplySurfaceFrame(mesh, verts, tris, planetCenterLocal, stablePoleAxis);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void ApplySurfaceFrame(
            Mesh mesh,
            List<Vector3> verts,
            List<int> tris,
            Vector3 planetCenterLocal,
            Vector3 stablePoleAxis)
        {
            var normals = new Vector3[verts.Count];
            var uvs = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                normals[i] = PlanetSurfaceFrame.OutwardNormal(verts[i], planetCenterLocal);
                uvs[i] = PlanetSurfaceFrame.WorldToSphericalUv(
                    verts[i], planetCenterLocal, stablePoleAxis, 0f);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
        }
    }
}
