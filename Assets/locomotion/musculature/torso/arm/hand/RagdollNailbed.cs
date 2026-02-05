using UnityEngine;

namespace Locomotion.Musculature
{
    /// <summary>
    /// Optional nailbed mesh for the caboose digit. Two modes: curve-based (ribbon along curve only)
    /// or radial/fingernail (curve-driven shape with height and edges, more natural for humans).
    /// </summary>
    [ExecuteAlways]
    public sealed class RagdollNailbed : MonoBehaviour
    {
        public enum ShapeMode
        {
            CurveBased,
            Radial,
            Sheet
        }

        [Header("Shape")]
        [Tooltip("CurveBased = ribbon. Radial = nail-shaped cap. Sheet = rectangular with straight sides, length + 4 curves.")]
        public ShapeMode shapeMode = ShapeMode.CurveBased;

        [Tooltip("Width profile (0..1). CurveBased: width along length.")]
        public AnimationCurve widthProfile = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

        [Tooltip("CurveBased: length along curve. Radial: scale for curve radii.")]
        public float length = 0.03f;
        public float baseWidth = 0.012f;
        public float thickness = 0.0015f;
        [Range(2, 32)] public int segmentsLength = 8;
        [Range(3, 64)] public int segmentsRound = 16;

        [Header("Radial/Sheet: 4 curves")]
        [Tooltip("Curve along the Y axis along the edige of the fingernail, Radius at the free edge (tip) as we go around (v 0..1). Oval for nail tip.")]
        public AnimationCurve edgeCurve = AnimationCurve.EaseInOut(0f, 0.6f, 0.5f, 1f);
        [Tooltip("Curve down the Y axis at the cuticle, Radius at the cuticle (root) as we go around (v 0..1). Smaller = nail root.")]
        public AnimationCurve cuticleCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 0.3f);
        [Tooltip("Curve along the Y axis, across the fingernail, Scale around the nail (yaw, v 0..1). Use to pinch or bulge the oval. Default: parabola (0→~1→0) for normal dome.")]
        public AnimationCurve yawRoundCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.98f), new Keyframe(1f, 0f));
        [Tooltip("Curve down the X axis, down the fingernail, Dome height from cuticle to tip (u 0..1). Nail curves up toward tip. Default: 0 at cuticle → ~1 at tip so dome runs full length.")]
        public AnimationCurve pitchRoundCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0.98f));

        [Tooltip("Sheet only: scale the width at the free edge (tip). 1 = from edge curve; >1 = wider edge; use to avoid pinching.")]
        [Min(0.01f)]
        public float edgeWidth = 1f;

        [Tooltip("Sheet only: bow along the nail (parallel to the face). Cuticle curve positive = push toward cuticle (-Z); edge curve positive = push toward tip (+Z).")]
        public float bowStrength = 0.15f;

        [Tooltip("Sheet only: displacement from yaw and pitch curves, tangent to the nail, rotated 90° about Z so the bend reads as a swell. Curves 1:1 on width (yaw) and length (pitch).")]
        public float nailbedBow = 0f;

        [Header("Transform offset")]
        [Tooltip("3D rotation offset (euler degrees) applied after hand-based rotation.")]
        public Vector3 transformRotationOffset = Vector3.zero;

        [Tooltip("3D translate offset in local space (e.g. to align with nail).")]
        public Vector3 transformTranslateOffset = Vector3.zero;

        [Header("Hand alignment")]
        [Tooltip("When true, base Y rotation is set from hand: +90° right, -90° left. When false, no base Y rotation.")]
        public bool useRotationFromHand = true;

        public Material material;

        private MeshFilter mf;
        private MeshRenderer mr;
        private Mesh mesh;
        private bool needsRebuild = false;

        private void OnEnable()
        {
            EnsureComponents();
            RebuildMesh();
        }

        private void OnValidate()
        {
            length = Mathf.Max(0.0001f, length);
            baseWidth = Mathf.Max(0.0001f, baseWidth);
            thickness = Mathf.Max(0f, thickness);
            edgeWidth = Mathf.Max(0.01f, edgeWidth);
            segmentsLength = Mathf.Clamp(segmentsLength, 2, 32);
            segmentsRound = Mathf.Clamp(segmentsRound, 3, 64);
            EnsureComponents();
            needsRebuild = true;
        }

        private void Update()
        {
            // Rebuild mesh after OnValidate completes
            if (needsRebuild)
            {
                needsRebuild = false;
                RebuildMesh();
            }
        }

        private void EnsureComponents()
        {
            mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            if (material != null) mr.sharedMaterial = material;
        }

        public void RebuildMesh()
        {
            if (mf == null) EnsureComponents();
            if (mesh == null)
            {
                mesh = new Mesh { name = "NailbedMesh" };
                mesh.MarkDynamic();
            }

            Vector3[] verts;
            Vector2[] uvs;
            int[] tris;

            if (shapeMode == ShapeMode.CurveBased)
                BuildCurveBasedMesh(out verts, out uvs, out tris);
            else if (shapeMode == ShapeMode.Sheet)
                BuildSheetMesh(out verts, out uvs, out tris);
            else
                BuildRadialMesh(out verts, out uvs, out tris);

            // 3D rotation: hand-based Y then euler offset
            float yDeg = useRotationFromHand ? ResolveHandRotationY() : 0f;
            Quaternion rot = Quaternion.Euler(transformRotationOffset.x, transformRotationOffset.y + yDeg, transformRotationOffset.z);
            for (int i = 0; i < verts.Length; i++)
                verts[i] = rot * verts[i] + transformTranslateOffset;

            mesh.Clear();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
        }

        private float ResolveHandRotationY()
        {
            var hand = GetComponentInParent<RagdollHand>();
            if (hand != null)
                return hand.side == BodySide.Right ? 90f : -90f;
            return 0f;
        }

        private void BuildCurveBasedMesh(out Vector3[] verts, out Vector2[] uvs, out int[] tris)
        {
            int nL = segmentsLength;
            int vCount = (nL + 1) * 4;
            verts = new Vector3[vCount];
            uvs = new Vector2[vCount];
            int vi = 0;
            float halfThick = thickness * 0.5f;
            for (int i = 0; i <= nL; i++)
            {
                float t = i / (float)nL;
                float w = baseWidth * Mathf.Max(0f, widthProfile.Evaluate(t));
                float z = t * length;
                verts[vi + 0] = new Vector3(-w * 0.5f, halfThick, z);
                verts[vi + 1] = new Vector3(+w * 0.5f, halfThick, z);
                verts[vi + 2] = new Vector3(-w * 0.5f, -halfThick, z);
                verts[vi + 3] = new Vector3(+w * 0.5f, -halfThick, z);
                uvs[vi + 0] = new Vector2(0f, t);
                uvs[vi + 1] = new Vector2(1f, t);
                uvs[vi + 2] = new Vector2(0f, t);
                uvs[vi + 3] = new Vector2(1f, t);
                vi += 4;
            }
            tris = new int[nL * 6 * 2];
            int ti = 0;
            for (int i = 0; i < nL; i++)
            {
                int b = i * 4, n = (i + 1) * 4;
                tris[ti++] = b + 0; tris[ti++] = n + 0; tris[ti++] = n + 1;
                tris[ti++] = b + 0; tris[ti++] = n + 1; tris[ti++] = b + 1;
                tris[ti++] = b + 2; tris[ti++] = n + 3; tris[ti++] = n + 2;
                tris[ti++] = b + 2; tris[ti++] = b + 3; tris[ti++] = n + 3;
            }
        }

        private static float SoftMax4(float a, float b, float c, float d, float k = 6f)
        {
            float m = Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
            float ex = Mathf.Exp(k * (a - m)) + Mathf.Exp(k * (b - m)) + Mathf.Exp(k * (c - m)) + Mathf.Exp(k * (d - m));
            return m + (1f / k) * Mathf.Log(Mathf.Max(ex, 1e-7f));
        }

        private void BuildSheetMesh(out Vector3[] verts, out Vector2[] uvs, out int[] tris)
        {
            // Polar square: (u,v) in [0,1]^2. Sides (v=0, v=1) are straight; width(u) from curves at center only.
            // Each point height = pitch(u) + yaw(v) + edge(v) integrated in 3D via soft max of all four curves.
            int nU = segmentsLength;
            int nV = segmentsRound;
            int rowV = nV + 1;
            int topCount = (nU + 1) * rowV;
            int totalVerts = 2 * topCount;
            verts = new Vector3[totalVerts];
            uvs = new Vector2[totalVerts];
            float halfThick = thickness * 0.5f;

            float cuticleCenter = Mathf.Max(0.001f, cuticleCurve.Evaluate(0.5f));
            float edgeCenter = Mathf.Max(0.001f, edgeCurve.Evaluate(0.5f));

            float PitchPolarAverage(float vertexU, float positionZ)
            {
                float vertexPolar = positionZ / length;
                float curvePolar = Mathf.Max(0f, pitchRoundCurve.Evaluate(vertexU));
                return (vertexPolar + curvePolar) * 0.5f;
            }

            Vector3 SampleTopPosition(float su, float sv)
            {
                float wAtU = baseWidth * ((1f - su) * cuticleCenter + su * edgeCenter * edgeWidth);
                float cCut = cuticleCurve.Evaluate(sv);
                float cEdge = edgeCurve.Evaluate(sv);
                float bZ = (su * cEdge - (1f - su) * cCut) * bowStrength * length;
                float sz = su * length + bZ;
                sz += cCut * bowStrength * length;
                float sx = (sv - 0.5f) * wAtU;
                float c1 = Mathf.Max(0f, cCut);
                float c2 = Mathf.Max(0f, cEdge);
                float c3 = Mathf.Max(0f, yawRoundCurve.Evaluate(sv));
                float c4 = Mathf.Max(0f, PitchPolarAverage(su, sz));
                float hScale = SoftMax4(c1, c2, c3, c4);
                float sy = halfThick + thickness * hScale;
                return new Vector3(sx, sy, sz);
            }

            for (int iu = 0; iu <= nU; iu++)
            {
                float u = iu / (float)nU;
                float widthAtU = baseWidth * ((1f - u) * cuticleCenter + u * edgeCenter * edgeWidth);
                for (int iv = 0; iv <= nV; iv++)
                {
                    float v = iv / (float)nV;
                    float cCut = cuticleCurve.Evaluate(v);
                    float cEdge = edgeCurve.Evaluate(v);
                    float bowZ = (u * cEdge - (1f - u) * cCut) * bowStrength * length;
                    float z = u * length + bowZ + cCut * bowStrength * length;
                    float x = (v - 0.5f) * widthAtU;
                    float c1 = Mathf.Max(0f, cCut);
                    float c2 = Mathf.Max(0f, cEdge);
                    float c3 = Mathf.Max(0f, yawRoundCurve.Evaluate(v));
                    float c4 = Mathf.Max(0f, PitchPolarAverage(u, z));
                    float heightScale = SoftMax4(c1, c2, c3, c4);
                    float yTop = halfThick + thickness * heightScale;
                    float yBot = -halfThick;
                    int idx = iu * rowV + iv;
                    verts[idx] = new Vector3(x, yTop, z);
                    uvs[idx] = new Vector2(u, v);
                    verts[topCount + idx] = new Vector3(x, yBot, z);
                    uvs[topCount + idx] = new Vector2(u, v);
                }
            }

            if (nailbedBow != 0f)
            {
                float du = 1f / nU;
                float dv = 1f / nV;
                for (int iu = 0; iu <= nU; iu++)
                {
                    float u = iu / (float)nU;
                    float widthAtU = baseWidth * ((1f - u) * cuticleCenter + u * edgeCenter * edgeWidth);
                    for (int iv = 0; iv <= nV; iv++)
                    {
                        float v = iv / (float)nV;
                        Vector3 P0 = SampleTopPosition(u, v);
                        float u1 = Mathf.Min(1f, u + du);
                        float v1 = Mathf.Min(1f, v + dv);
                        Vector3 P1 = SampleTopPosition(u1, v);
                        Vector3 P2 = SampleTopPosition(u, v1);
                        Vector3 Tu = (P1 - P0) / Mathf.Max(0.0001f, u1 - u);
                        Vector3 Tv = (P2 - P0) / Mathf.Max(0.0001f, v1 - v);
                        Vector3 N = Vector3.Cross(Tu, Tv).normalized;
                        if (N.sqrMagnitude < 0.0001f) continue;
                        float yawV = Mathf.Max(0f, yawRoundCurve.Evaluate(v));
                        float pitchV = Mathf.Max(0f, PitchPolarAverage(u, P0.z));
                        Vector3 D = new Vector3(
                            yawV * (-nailbedBow) * widthAtU,
                            0f,
                            pitchV * (-nailbedBow) * length);
                        Vector3 D_tangent = D - N * Vector3.Dot(D, N);
                        Vector3 D_swell = new Vector3(-D_tangent.y, D_tangent.x, D_tangent.z);
                        int idx = iu * rowV + iv;
                        verts[idx] += D_swell;
                        verts[topCount + idx] += D_swell;
                    }
                }
            }

            int topBotQuads = nU * nV;
            int sideQuads = 2 * nV + 2 * nU;
            int triCount = (topBotQuads * 4 + sideQuads * 2) * 3;
            tris = new int[triCount];
            int ti = 0;

            int Top(int iu, int iv) => iu * rowV + iv;
            int Bot(int iu, int iv) => topCount + iu * rowV + iv;

            for (int iu = 0; iu < nU; iu++)
            for (int iv = 0; iv < nV; iv++)
            {
                tris[ti++] = Top(iu, iv);
                tris[ti++] = Top(iu + 1, iv);
                tris[ti++] = Top(iu + 1, iv + 1);
                tris[ti++] = Top(iu, iv);
                tris[ti++] = Top(iu + 1, iv + 1);
                tris[ti++] = Top(iu, iv + 1);
                tris[ti++] = Bot(iu, iv);
                tris[ti++] = Bot(iu, iv + 1);
                tris[ti++] = Bot(iu + 1, iv + 1);
                tris[ti++] = Bot(iu, iv);
                tris[ti++] = Bot(iu + 1, iv + 1);
                tris[ti++] = Bot(iu + 1, iv);
            }
            for (int iv = 0; iv < nV; iv++)
            {
                tris[ti++] = Top(0, iv);
                tris[ti++] = Bot(0, iv);
                tris[ti++] = Bot(0, iv + 1);
                tris[ti++] = Top(0, iv);
                tris[ti++] = Bot(0, iv + 1);
                tris[ti++] = Top(0, iv + 1);
                tris[ti++] = Top(nU, iv);
                tris[ti++] = Bot(nU, iv + 1);
                tris[ti++] = Top(nU, iv + 1);
                tris[ti++] = Top(nU, iv);
                tris[ti++] = Bot(nU, iv);
                tris[ti++] = Bot(nU, iv + 1);
            }
            for (int iu = 0; iu < nU; iu++)
            {
                tris[ti++] = Top(iu, 0);
                tris[ti++] = Bot(iu, 0);
                tris[ti++] = Bot(iu + 1, 0);
                tris[ti++] = Top(iu, 0);
                tris[ti++] = Bot(iu + 1, 0);
                tris[ti++] = Top(iu + 1, 0);
                tris[ti++] = Top(iu, nV);
                tris[ti++] = Bot(iu, nV);
                tris[ti++] = Bot(iu + 1, nV);
                tris[ti++] = Top(iu, nV);
                tris[ti++] = Bot(iu + 1, nV);
                tris[ti++] = Top(iu + 1, nV);
            }
        }

        private void BuildRadialMesh(out Vector3[] verts, out Vector2[] uvs, out int[] tris)
        {
            // Filled nail-shaped surface: center at cuticle (u=0), rings to edge (u=1). No hole - continuous parametric cap.
            int nU = segmentsLength;
            int nV = segmentsRound;
            int ringV = nV;
            int topCount = 1 + nU * ringV;
            int totalVerts = 2 * topCount;
            verts = new Vector3[totalVerts];
            uvs = new Vector2[totalVerts];
            float halfThick = thickness * 0.5f;

            float yTop0 = halfThick + thickness * Mathf.Max(0f, pitchRoundCurve.Evaluate(0f));
            float yBot0 = -halfThick;
            verts[0] = new Vector3(0f, yTop0, 0f);
            uvs[0] = new Vector2(0f, 0.5f);
            verts[topCount] = new Vector3(0f, yBot0, 0f);
            uvs[topCount] = new Vector2(0f, 0.5f);

            for (int iu = 1; iu <= nU; iu++)
            {
                float u = iu / (float)nU;
                float yTop = halfThick + thickness * Mathf.Max(0f, pitchRoundCurve.Evaluate(u));
                float yBot = -halfThick;
                for (int iv = 0; iv < nV; iv++)
                {
                    float v = iv / (float)nV;
                    float rCut = baseWidth * Mathf.Max(0.001f, cuticleCurve.Evaluate(v));
                    float rEdge = baseWidth * Mathf.Max(0.001f, edgeCurve.Evaluate(v));
                    float r = ((1f - u) * rCut + u * rEdge) * Mathf.Max(0.01f, yawRoundCurve.Evaluate(v));
                    float a = v * 2f * Mathf.PI;
                    float x = r * Mathf.Cos(a);
                    float z = r * Mathf.Sin(a);
                    int idx = 1 + (iu - 1) * ringV + iv;
                    verts[idx] = new Vector3(x, yTop, z);
                    uvs[idx] = new Vector2(u, v);
                    verts[topCount + idx] = new Vector3(x, yBot, z);
                    uvs[topCount + idx] = new Vector2(u, v);
                }
            }

            int fanTris = nV;
            int quadTris = (nU - 1) * nV * 2;
            int topBottomTris = (fanTris + quadTris) * 2;
            int edgeSideTris = nV * 2;
            int triCount = (topBottomTris + edgeSideTris) * 3;
            tris = new int[triCount];
            int ti = 0;

            int Top(int iu, int iv)
            {
                if (iu == 0) return 0;
                return 1 + (iu - 1) * ringV + (iv % ringV + ringV) % ringV;
            }
            int Bot(int iu, int iv) => topCount + Top(iu, iv);

            for (int iv = 0; iv < nV; iv++)
            {
                int iv1 = (iv + 1) % nV;
                tris[ti++] = Top(0, 0);
                tris[ti++] = Top(1, iv1);
                tris[ti++] = Top(1, iv);
                tris[ti++] = Bot(0, 0);
                tris[ti++] = Bot(1, iv);
                tris[ti++] = Bot(1, iv1);
            }
            for (int iu = 1; iu < nU; iu++)
            for (int iv = 0; iv < nV; iv++)
            {
                int iv1 = (iv + 1) % nV;
                tris[ti++] = Top(iu, iv);
                tris[ti++] = Top(iu + 1, iv);
                tris[ti++] = Top(iu + 1, iv1);
                tris[ti++] = Top(iu, iv);
                tris[ti++] = Top(iu + 1, iv1);
                tris[ti++] = Top(iu, iv1);
                tris[ti++] = Bot(iu, iv);
                tris[ti++] = Bot(iu, iv1);
                tris[ti++] = Bot(iu + 1, iv1);
                tris[ti++] = Bot(iu, iv);
                tris[ti++] = Bot(iu + 1, iv1);
                tris[ti++] = Bot(iu + 1, iv);
            }
            for (int iv = 0; iv < nV; iv++)
            {
                int iv1 = (iv + 1) % nV;
                tris[ti++] = Top(nU, iv);
                tris[ti++] = Top(nU, iv1);
                tris[ti++] = Bot(nU, iv1);
                tris[ti++] = Top(nU, iv);
                tris[ti++] = Bot(nU, iv1);
                tris[ti++] = Bot(nU, iv);
            }
        }
    }
}
