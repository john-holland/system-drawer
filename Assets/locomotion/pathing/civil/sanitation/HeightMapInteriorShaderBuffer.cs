using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks meshes that descend into a heightmap; prebakes cutouts; invalidates on OnMove.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Height Map Interior Shader Buffer")]
public sealed class HeightMapInteriorShaderBuffer : MonoBehaviour
{
    public Texture2D heightMap;
    public float heightAmplitude = 4f;
    public Material targetMaterial;
    public string heightMapProperty = "_GumHeightMap";
    public List<MeshRenderer> descendingMeshes = new List<MeshRenderer>();
    public List<Rect> removedQuads = new List<Rect>();
    public bool dirty = true;
    MaterialPropertyBlock _mpb;
    readonly Dictionary<int, TransformSnapshot> _snapshots = new Dictionary<int, TransformSnapshot>();

    MaterialPropertyBlock PropertyBlock => _mpb ??= new MaterialPropertyBlock();

    struct TransformSnapshot
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
    }

    void Awake() => Prebake();

    void LateUpdate()
    {
        for (int i = 0; i < descendingMeshes.Count; i++)
        {
            var mr = descendingMeshes[i];
            if (mr == null) continue;
            int id = mr.GetInstanceID();
            var t = mr.transform;
            if (!_snapshots.TryGetValue(id, out var snap)
                || snap.pos != t.position || snap.rot != t.rotation || snap.scale != t.lossyScale)
            {
                OnMove(mr);
                _snapshots[id] = new TransformSnapshot
                {
                    pos = t.position,
                    rot = t.rotation,
                    scale = t.lossyScale
                };
            }
        }
        if (dirty)
            Prebake();
    }

    public void RegisterDescendingMesh(MeshRenderer mr)
    {
        if (mr == null || descendingMeshes.Contains(mr)) return;
        descendingMeshes.Add(mr);
        dirty = true;
    }

    public void UnregisterDescendingMesh(MeshRenderer mr)
    {
        if (mr == null) return;
        descendingMeshes.Remove(mr);
        _snapshots.Remove(mr.GetInstanceID());
        dirty = true;
    }

    public void OnMove(MeshRenderer mr)
    {
        if (mr == null) return;
        RemoveVolumeIntersectQuads(mr);
        dirty = true;
        SendMessage("OnHeightMapInteriorMoved", mr, SendMessageOptions.DontRequireReceiver);
    }

    public void RemoveVolumeIntersectQuads(MeshRenderer mr)
    {
        if (mr == null) return;
        Bounds b = mr.bounds;
        var r = new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
        for (int i = removedQuads.Count - 1; i >= 0; i--)
            if (RectsOverlap(removedQuads[i], r))
                removedQuads.RemoveAt(i);
        removedQuads.Add(r);
    }

    static bool RectsOverlap(Rect a, Rect b) =>
        a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;

    public void Prebake()
    {
        dirty = false;
        if (heightMap == null)
        {
            heightMap = new Texture2D(64, 64, TextureFormat.RFloat, false);
            heightMap.name = "HeightMapInteriorBake";
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.black;
            heightMap.SetPixels(pixels);
            heightMap.Apply();
        }

        // Stamp removed quads as zero-height cutouts in UV space of this transform bounds.
        Bounds world = new Bounds(transform.position, Vector3.one * 40f);
        for (int i = 0; i < descendingMeshes.Count; i++)
            if (descendingMeshes[i] != null)
                world.Encapsulate(descendingMeshes[i].bounds);

        var cols = heightMap.GetPixels();
        int w = heightMap.width;
        int h = heightMap.height;
        for (int q = 0; q < removedQuads.Count; q++)
        {
            var r = removedQuads[q];
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(world.min.x, world.max.x, r.xMin) * (w - 1)), 0, w - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.InverseLerp(world.min.x, world.max.x, r.xMax) * (w - 1)), 0, w - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(world.min.z, world.max.z, r.yMin) * (h - 1)), 0, h - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.InverseLerp(world.min.z, world.max.z, r.yMax) * (h - 1)), 0, h - 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                cols[y * w + x] = Color.black;
        }
        heightMap.SetPixels(cols);
        heightMap.Apply(false);

        if (targetMaterial != null && targetMaterial.HasProperty(heightMapProperty))
            targetMaterial.SetTexture(heightMapProperty, heightMap);
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.GetPropertyBlock(PropertyBlock);
            PropertyBlock.SetTexture(heightMapProperty, heightMap);
            rend.SetPropertyBlock(PropertyBlock);
        }
    }
}
