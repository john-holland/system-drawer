using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    /// <summary>
    /// Shrink-wrap ribbon mesh sampler along a RoadSpline3D with UV layout and optional underside loop.
    /// </summary>
    [AddComponentMenu("Roads/Spline Path Mesh Sampler")]
    [RequireComponent(typeof(RoadSpline3D))]
    public class SplinePathMeshSampler : MonoBehaviour
    {
        public RoadSpline3D spline;
        public float sampleSpacingMeters = 1f;
        public bool closeUndersideWithLoop;
        public float undersideDropMeters = 2f;
        public float uvTileLengthMeters = 4f;
        public int ribbonSubdivisions = 4;
        public LayerMask terrainLayers = ~0;

        public Mesh BuildRibbonMesh()
        {
            var samples = BuildPathSamples();
            if (samples.Count < 2)
                return new Mesh();

            int lateralVerts = ribbonSubdivisions + 1;
            int vertCount = samples.Count * lateralVerts;
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var normals = new Vector3[vertCount];
            var tris = new List<int>();

            float tileLen = Mathf.Max(0.5f, uvTileLengthMeters);
            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                for (int j = 0; j < lateralVerts; j++)
                {
                    float across = (float)j / ribbonSubdivisions - 0.5f;
                    int idx = i * lateralVerts + j;
                    verts[idx] = transform.InverseTransformPoint(
                        s.position + s.binormal * across * (s.widthLeft + s.widthRight) * 0.5f * 2f);
                    uvs[idx] = new Vector2(s.uvAlong / tileLen, across + 0.5f);
                    normals[idx] = transform.InverseTransformDirection(s.normal);
                }
            }

            for (int i = 0; i < samples.Count - 1; i++)
            {
                for (int j = 0; j < ribbonSubdivisions; j++)
                {
                    int a = i * lateralVerts + j;
                    int b = a + 1;
                    int c = a + lateralVerts;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            var mesh = new Mesh { name = "RoadRibbon" };
            mesh.SetVertices(new List<Vector3>(verts));
            mesh.SetUVs(0, new List<Vector2>(uvs));
            mesh.SetNormals(new List<Vector3>(normals));
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public Mesh BuildUndersideLoopMesh()
        {
            var samples = BuildPathSamples();
            if (samples.Count < 2)
                return new Mesh();

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float tileLen = Mathf.Max(0.5f, uvTileLengthMeters);

            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                if (!s.overhang && !closeUndersideWithLoop)
                    continue;

                float halfW = (s.widthLeft + s.widthRight) * 0.5f;
                Vector3 left = s.position - s.binormal * halfW;
                Vector3 right = s.position + s.binormal * halfW;
                Vector3 down = s.position - s.normal * undersideDropMeters;

                int baseIdx = verts.Count;
                verts.Add(transform.InverseTransformPoint(left));
                verts.Add(transform.InverseTransformPoint(right));
                verts.Add(transform.InverseTransformPoint(down));
                uvs.Add(new Vector2(s.uvAlong / tileLen, 0f));
                uvs.Add(new Vector2(s.uvAlong / tileLen, 1f));
                uvs.Add(new Vector2(s.uvAlong / tileLen, 0.5f));

                if (i < samples.Count - 1)
                {
                    var sn = samples[i + 1];
                    float halfWn = (sn.widthLeft + sn.widthRight) * 0.5f;
                    Vector3 leftN = sn.position - sn.binormal * halfWn;
                    Vector3 rightN = sn.position + sn.binormal * halfWn;
                    Vector3 downN = sn.position - sn.normal * undersideDropMeters;

                    int nextBase = verts.Count;
                    verts.Add(transform.InverseTransformPoint(leftN));
                    verts.Add(transform.InverseTransformPoint(rightN));
                    verts.Add(transform.InverseTransformPoint(downN));
                    uvs.Add(new Vector2(sn.uvAlong / tileLen, 0f));
                    uvs.Add(new Vector2(sn.uvAlong / tileLen, 1f));
                    uvs.Add(new Vector2(sn.uvAlong / tileLen, 0.5f));

                    // Left skirt
                    tris.Add(baseIdx); tris.Add(nextBase); tris.Add(baseIdx + 2);
                    tris.Add(baseIdx + 2); tris.Add(nextBase); tris.Add(nextBase + 2);
                    // Right skirt
                    tris.Add(baseIdx + 1); tris.Add(baseIdx + 1 + 3); tris.Add(nextBase + 1);
                    tris.Add(baseIdx + 1 + 3); tris.Add(nextBase + 1 + 3); tris.Add(nextBase + 1);
                    // Bottom loop
                    tris.Add(baseIdx + 2); tris.Add(nextBase + 2); tris.Add(baseIdx + 1 + 3);
                    tris.Add(baseIdx + 2); tris.Add(baseIdx + 1 + 3); tris.Add(nextBase + 2);
                }
            }

            if (verts.Count < 3)
                return new Mesh();

            var mesh = new Mesh { name = "RoadUndersideLoop" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public Mesh BuildCombinedMesh()
        {
            var ribbon = BuildRibbonMesh();
            if (!closeUndersideWithLoop)
                return ribbon;

            var underside = BuildUndersideLoopMesh();
            return CombineMeshes(ribbon, underside);
        }

        public static Mesh CombineMeshes(Mesh a, Mesh b)
        {
            if (a == null || a.vertexCount == 0) return b;
            if (b == null || b.vertexCount == 0) return a;
            var combined = new Mesh { name = "RoadCombined" };
            combined.CombineMeshes(new[]
            {
                new CombineInstance { mesh = a, transform = Matrix4x4.identity },
                new CombineInstance { mesh = b, transform = Matrix4x4.identity }
            }, true, false);
            combined.RecalculateBounds();
            return combined;
        }

        public Bounds ComputeWorldBounds()
        {
            var mesh = BuildCombinedMesh();
            if (mesh.vertexCount == 0)
                return new Bounds(transform.position, Vector3.one);
            return mesh.bounds;
        }

        public List<SplinePathSample> BuildPathSamples()
        {
            if (spline == null)
                spline = GetComponent<RoadSpline3D>();
            if (spline == null)
                return new List<SplinePathSample>();

            spline.RebuildBakedSamples(sampleSpacingMeters);
            var baked = spline.BakedSamples;
            if (baked == null || baked.Count == 0)
                return new List<SplinePathSample>();

            var result = new List<SplinePathSample>(baked.Count);
            foreach (var bakedSample in baked)
            {
                var sample = bakedSample;
                bool overhang = spline.IsOverhangAt(sample);
                ShrinkWrapSample(ref sample, overhang);
                result.Add(new SplinePathSample
                {
                    distance = sample.distance,
                    uvAlong = sample.distance,
                    uvAcross = 0f,
                    position = sample.position,
                    tangent = sample.tangent,
                    normal = sample.normal,
                    binormal = sample.binormal,
                    widthLeft = sample.width * 0.5f,
                    widthRight = sample.width * 0.5f,
                    overhang = overhang
                });
            }
            return result;
        }

        void ShrinkWrapSample(ref RoadSplineSample sample, bool overhang)
        {
            Vector3 origin = sample.position + Vector3.up * 50f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, terrainLayers, QueryTriggerInteraction.Ignore))
            {
                sample.position = new Vector3(sample.position.x, hit.point.y + 0.05f, sample.position.z);
            }
            if (overhang && closeUndersideWithLoop)
            {
                // Preserve banking frame; height already set from terrain conform
            }
        }

        void Reset()
        {
            spline = GetComponent<RoadSpline3D>();
        }
    }
}
