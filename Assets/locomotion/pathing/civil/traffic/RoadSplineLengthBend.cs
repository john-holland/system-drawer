using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Bends a mesh along a road spline (CPU collider + MaterialPropertyBlock for Roads/SplineLengthBend).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Spline Length Bend")]
public sealed class RoadSplineLengthBend : MonoBehaviour
{
    public bool bendWithRoad = true;
    public MonoBehaviour spline;
    public float lateralOffsetM;
    public float meshLength = 4f;
    public bool laneDisabled;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    Texture2D _sampleTex;

    public void BindSpline(MonoBehaviour roadSpline, float lateral)
    {
        spline = roadSpline;
        lateralOffsetM = lateral;
        Rebuild();
    }

    public void Rebuild()
    {
        if (!bendWithRoad || spline == null)
            return;
        var samples = SampleSpline(0.5f);
        if (samples.Count < 2) return;
        UploadSampleTex(samples);
        BakeCollider(samples);
        var rend = GetComponent<MeshRenderer>() ?? GetComponentInChildren<MeshRenderer>();
        if (rend != null && _sampleTex != null)
        {
            var block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            block.SetTexture("_SplineSamples", _sampleTex);
            block.SetFloat("_SampleCount", samples.Count);
            block.SetFloat("_MeshLength", meshLength);
            rend.SetPropertyBlock(block);
        }
    }

    public List<Vector3> BentPositionsAlongZ(int count)
    {
        var list = new List<Vector3>();
        var samples = SampleSpline(Mathf.Max(0.25f, meshLength / Mathf.Max(2, count)));
        int n = Mathf.Max(2, count);
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : i / (float)(n - 1);
            int idx = Mathf.Clamp(Mathf.RoundToInt(t * (samples.Count - 1)), 0, Mathf.Max(0, samples.Count - 1));
            if (samples.Count == 0) list.Add(transform.position);
            else list.Add(samples[idx].pos + samples[idx].binormal * lateralOffsetM);
        }
        return list;
    }

    struct Sample
    {
        public Vector3 pos, tangent, binormal, normal;
    }

    List<Sample> SampleSpline(float spacing)
    {
        var list = new List<Sample>();
        if (spline == null) return list;
        var getLen = spline.GetType().GetMethod("GetTotalLength", BindingFlags.Instance | BindingFlags.Public);
        var getAt = spline.GetType().GetMethod("GetSampleAtDistance", BindingFlags.Instance | BindingFlags.Public);
        if (getLen == null || getAt == null) return list;
        float total = (float)getLen.Invoke(spline, null);
        for (float d = 0f; d <= total; d += Mathf.Max(0.25f, spacing))
        {
            object s = getAt.Invoke(spline, new object[] { d });
            if (s == null) continue;
            var t = s.GetType();
            list.Add(new Sample
            {
                pos = (Vector3)(t.GetField("position")?.GetValue(s) ?? Vector3.zero),
                tangent = (Vector3)(t.GetField("tangent")?.GetValue(s) ?? Vector3.forward),
                binormal = (Vector3)(t.GetField("binormal")?.GetValue(s) ?? Vector3.right),
                normal = (Vector3)(t.GetField("normal")?.GetValue(s) ?? Vector3.up)
            });
        }
        return list;
    }

    void UploadSampleTex(List<Sample> samples)
    {
        int n = Mathf.Max(2, samples.Count);
        _sampleTex = new Texture2D(n, 4, TextureFormat.RGBAFloat, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        for (int i = 0; i < n; i++)
        {
            var s = samples[Mathf.Min(i, samples.Count - 1)];
            _sampleTex.SetPixel(i, 0, new Color(s.pos.x, s.pos.y, s.pos.z, 1f));
            _sampleTex.SetPixel(i, 1, new Color(s.tangent.x, s.tangent.y, s.tangent.z, 0f));
            _sampleTex.SetPixel(i, 2, new Color(s.binormal.x, s.binormal.y, s.binormal.z, 0f));
            _sampleTex.SetPixel(i, 3, new Color(s.normal.x, s.normal.y, s.normal.z, 0f));
        }
        _sampleTex.Apply();
    }

    void BakeCollider(List<Sample> samples)
    {
        if (meshCollider == null)
            meshCollider = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        var mesh = new Mesh { name = "SplineBendCollider" };
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            Vector3 p = transform.InverseTransformPoint(s.pos + s.binormal * lateralOffsetM);
            verts.Add(p + Vector3.up * 0.4f);
            verts.Add(p - Vector3.up * 0.05f);
            if (i > 0)
            {
                int a = (i - 1) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
            }
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh;
    }

    public static int PlaceStraightModules(Transform parent, GameObject prefab, IList<Vector3> along, float moduleLength)
    {
        if (along == null || along.Count < 2) return 0;
        float acc = 0f;
        int n = 0;
        for (int i = 1; i < along.Count; i++)
        {
            acc += Vector3.Distance(along[i - 1], along[i]);
            if (acc >= moduleLength)
            {
                var go = prefab != null
                    ? Object.Instantiate(prefab, along[i], Quaternion.LookRotation(along[i] - along[i - 1]), parent)
                    : new GameObject("Module_" + n);
                if (prefab == null)
                {
                    go.transform.SetParent(parent, false);
                    go.transform.position = along[i];
                }
                n++;
                acc = 0f;
            }
        }
        return Mathf.Max(1, n);
    }
}
