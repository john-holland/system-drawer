using UnityEngine;
using SdfMax;

public struct InkNibBreakResult
{
    public bool broke;
    public float stress01;
    public float clampedBendDeg;
    public float requestedBendDeg;
    public Vector3 breakWorld;
    public GameObject debris;
    public int hydroSeeded;
}

/// <summary>
/// Always runs on contact. ICT leaves pick the break chunk; stress ≥ 1 expands aperture and replaces the nib.
/// Splatter runs regardless of break.
/// </summary>
public static class InkNibBreakAnalyzer
{
    public static InkNibBreakResult OnContact(PenInkInstrument instrument, PaintCanvas canvas,
        float requestedBendDeg, float contactForceN, Collider contactCollider = null, Vector3 contactNormal = default,
        bool splatter = true)
    {
        var result = new InkNibBreakResult
        {
            requestedBendDeg = requestedBendDeg
        };
        if (instrument == null)
            return result;

        var nib = instrument.ResolveNib();
        result.clampedBendDeg = nib.ClampBendDeg(requestedBendDeg);
        float breakForce = Mathf.Max(0.1f, instrument.breakForceN);
        result.stress01 = nib.Stress01(requestedBendDeg, contactForceN, breakForce);

        Vector3 tip = instrument.TipWorld;
        result.breakWorld = tip;
        Bounds leafBounds = default;
        bool hasLeaf = TryPickBreakLeaf(instrument, out leafBounds, out result.breakWorld);
        if (!hasLeaf)
            result.breakWorld = tip;

        if (result.stress01 >= 1f)
        {
            result.broke = true;
            instrument.nibBroken = true;
            result.debris = SpawnDebris(instrument, result.breakWorld, hasLeaf ? leafBounds.size : Vector3.one * 0.008f);
            ReplaceNib(nib);
            instrument.ExpandAperture(Mathf.Max(nib.apertureRadiusM * 3f, 0.004f));
        }

        if (splatter)
            InkSphContactSplatter.Splatter(instrument, canvas, tip, contactNormal, contactCollider);
        if (canvas != null)
        {
            var hydro = canvas.GetComponent<PaintCanvasHydroSolver>() ?? canvas.Hydro;
            if (hydro != null)
                result.hydroSeeded = hydro.ActiveCount;
        }
        return result;
    }

    static bool TryPickBreakLeaf(PenInkInstrument instrument, out Bounds leafBounds, out Vector3 world)
    {
        leafBounds = default;
        world = instrument.TipWorld;
        var nib = instrument.ResolveNib();
        var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        comp.nodes = new System.Collections.Generic.List<SdfMaxNode>
        {
            new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Capsule,
                sphereRadius = Mathf.Max(0.0004f, nib.apertureRadiusM),
                radius = Mathf.Max(0.0004f, nib.apertureRadiusM),
                localPosition = Vector3.zero,
                extrusionEnd = Vector3.forward * nib.nibLengthM
            }
        };
        comp.rootNodeIndex = 0;

        var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
        profile.maxDepth = 4;
        profile.minLeafExtent = 0.002f;
        profile.sampleEpsilon = 0.0002f;
        profile.enablePlanarContext = false;

        Matrix4x4 l2w = instrument.tip != null
            ? instrument.tip.localToWorldMatrix
            : instrument.transform.localToWorldMatrix;
        var graph = new SdfMaxExpressionGraph(comp, profile, l2w);
        var eval = new SdfMaxEvaluator(graph);
        var ict = new IntegralConvexTreeSolver();
        Vector3 size = new Vector3(nib.apertureRadiusM * 8f, nib.apertureRadiusM * 8f, nib.nibLengthM * 1.2f);
        Bounds worldBounds = new Bounds(l2w.MultiplyPoint3x4(Vector3.forward * (nib.nibLengthM * 0.5f)), size);
        ict.Build(eval, worldBounds, profile);

        bool found = false;
        float best = -1f;
        for (int i = 0; i < ict.Leaves.Count; i++)
        {
            var leaf = ict.Leaves[i];
            if (leaf.IntegratedMeasure >= best)
            {
                best = leaf.IntegratedMeasure;
                leafBounds = leaf.LeafBounds;
                world = leaf.LeafBounds.center;
                found = true;
            }
        }

        DestroySo(comp);
        DestroySo(profile);
        return found;
    }

    static void ReplaceNib(QuillNibDefinition nib)
    {
        nib.nibLengthM = Mathf.Max(0.004f, nib.nibLengthM * 0.55f);
        nib.apertureRadiusM = Mathf.Max(nib.apertureRadiusM * 2.5f, 0.003f);
        nib.tipHold = Mathf.Min(nib.tipHold, 0.4f);
    }

    static GameObject SpawnDebris(PenInkInstrument instrument, Vector3 world, Vector3 size)
    {
        var go = new GameObject("InkNibDebris");
        go.transform.position = world;
        go.transform.rotation = instrument.tip != null ? instrument.tip.rotation : instrument.transform.rotation;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = BuildChunk(size);
        go.AddComponent<MeshRenderer>();
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.002f;
        rb.AddForce((instrument.TipForward + Vector3.up * 0.2f) * 0.4f, ForceMode.Impulse);
        return go;
    }

    static Mesh BuildChunk(Vector3 size)
    {
        var mesh = new Mesh { name = "InkNibChunk" };
        float x = Mathf.Max(0.001f, size.x * 0.5f);
        float y = Mathf.Max(0.001f, size.y * 0.5f);
        float z = Mathf.Max(0.001f, size.z * 0.5f);
        mesh.vertices = new[]
        {
            new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z),
            new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(-x, y, z)
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    static void DestroySo(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
