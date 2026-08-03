using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Roadside SG-style decorator: best-fit poles along road shoulder with sublimation
/// toward existing content; ground-merge junctions for underground spans by default.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Power Line Roadside Decorator")]
public sealed class PowerLineRoadsideDecorator : MonoBehaviour
{
    [Tooltip("RoadSpline3D or any component with BuildSamples(float) / GetSampleAtDistance(float).")]
    public MonoBehaviour roadSpline;
    public float poleSpacingM = 35f;
    public float shoulderOffsetM = 4.5f;
    public bool rightSide = true;
    public bool useGroundMergeJunctions = true;
    public float sublimationProbeRadiusM = 1.2f;
    public LayerMask occupancyMask = ~0;
    public Transform polesRoot;
    public bool generateOnAwake = true;
    public PowerLineTensionLemma defaultTensionLemma;

    public readonly List<UtilityPoleAssembly> poles = new List<UtilityPoleAssembly>();
    public readonly List<PowerLineSpan> spans = new List<PowerLineSpan>();
    public readonly List<string> warnings = new List<string>();

    void Awake()
    {
        if (polesRoot == null)
        {
            var go = new GameObject("PowerPoles");
            go.transform.SetParent(transform, false);
            polesRoot = go.transform;
        }
        if (generateOnAwake && roadSpline != null)
            Generate();
    }

    [ContextMenu("Generate Power Lines")]
    public void Generate()
    {
        warnings.Clear();
        ClearGenerated();
        if (roadSpline == null)
        {
            warnings.Add("No roadSpline assigned.");
            Debug.LogWarning("[PowerLineRoadsideDecorator] " + warnings[0]);
            return;
        }

        var samples = SampleRoad(poleSpacingM);
        if (samples.Count < 2)
        {
            warnings.Add("Road sampling produced fewer than 2 points — cannot connect optimally.");
            Debug.LogWarning("[PowerLineRoadsideDecorator] " + warnings[0]);
            return;
        }

        UtilityPoleAssembly prev = null;
        bool lastWasUnderground = false;
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            Vector3 side = (rightSide ? 1f : -1f) * s.binormal.normalized;
            Vector3 pos = s.position + side * (s.width * 0.5f + shoulderOffsetM);

            if (IsOccupied(pos))
            {
                // Sublimation: nudge outward
                Vector3 nudged = pos + side * sublimationProbeRadiusM * 1.5f;
                if (IsOccupied(nudged))
                {
                    warnings.Add($"Sublimation failed at sample {i} — using ground-merge skip.");
                    if (useGroundMergeJunctions && prev != null && !lastWasUnderground)
                    {
                        // Pretend underground; next successful pole reconnects
                        lastWasUnderground = true;
                    }
                    continue;
                }
                pos = nudged;
            }

            var pole = CreatePole(pos, s.tangent);
            poles.Add(pole);
            if (prev != null)
            {
                if (lastWasUnderground && useGroundMergeJunctions)
                {
                    // Rise from ground merge — no long aerial span across gap
                    lastWasUnderground = false;
                }
                else
                {
                    spans.Add(CreateSpan(prev, pole));
                }
            }
            prev = pole;
        }

        if (poles.Count < 2)
            warnings.Add("Unable to achieve optimal pole connection (need ≥2 poles).");

        for (int w = 0; w < warnings.Count; w++)
            Debug.LogWarning("[PowerLineRoadsideDecorator] " + warnings[w], this);
    }

    struct ShoulderSample
    {
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 binormal;
        public float width;
    }

    List<ShoulderSample> SampleRoad(float spacing)
    {
        var list = new List<ShoulderSample>();
        var type = roadSpline.GetType();
        var build = type.GetMethod("BuildSamples", BindingFlags.Instance | BindingFlags.Public);
        if (build != null)
        {
            var arr = build.Invoke(roadSpline, new object[] { spacing }) as System.Array;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    var item = arr.GetValue(i);
                    if (item == null) continue;
                    list.Add(ReadSample(item));
                }
                return list;
            }
        }

        var getLen = type.GetMethod("GetTotalLength", BindingFlags.Instance | BindingFlags.Public);
        var getAt = type.GetMethod("GetSampleAtDistance", BindingFlags.Instance | BindingFlags.Public);
        if (getLen != null && getAt != null)
        {
            float total = (float)getLen.Invoke(roadSpline, null);
            for (float d = 0f; d <= total; d += Mathf.Max(5f, spacing))
                list.Add(ReadSample(getAt.Invoke(roadSpline, new object[] { d })));
        }
        return list;
    }

    static ShoulderSample ReadSample(object sample)
    {
        var t = sample.GetType();
        return new ShoulderSample
        {
            position = (Vector3)(t.GetField("position")?.GetValue(sample) ?? Vector3.zero),
            tangent = (Vector3)(t.GetField("tangent")?.GetValue(sample) ?? Vector3.forward),
            binormal = (Vector3)(t.GetField("binormal")?.GetValue(sample) ?? Vector3.right),
            width = (float)(t.GetField("width")?.GetValue(sample) ?? 6f)
        };
    }

    bool IsOccupied(Vector3 worldPos)
    {
        return Physics.CheckSphere(worldPos + Vector3.up * 1f, sublimationProbeRadiusM, occupancyMask, QueryTriggerInteraction.Ignore);
    }

    UtilityPoleAssembly CreatePole(Vector3 pos, Vector3 tangent)
    {
        var go = new GameObject("UtilityPole");
        go.transform.SetParent(polesRoot, false);
        go.transform.position = pos;
        if (tangent.sqrMagnitude > 1e-4f)
            go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(tangent, Vector3.up).normalized, Vector3.up);
        var pole = go.AddComponent<UtilityPoleAssembly>();
        if (defaultTensionLemma != null)
        {
            var lemma = go.AddComponent<PowerLineTensionLemma>();
            lemma.lemma = defaultTensionLemma.lemma;
            lemma.leanBias01 = defaultTensionLemma.leanBias01;
            lemma.poleBreakChance01 = defaultTensionLemma.poleBreakChance01;
            pole.tensionLemma = lemma;
        }
        pole.EnsureVisuals();
        return pole;
    }

    PowerLineSpan CreateSpan(UtilityPoleAssembly a, UtilityPoleAssembly b)
    {
        var go = new GameObject($"Span_{a.name}_{b.name}");
        go.transform.SetParent(polesRoot, false);
        var span = go.AddComponent<PowerLineSpan>();
        var from = new GameObject("From").transform;
        from.SetParent(a.transform, false);
        from.position = a.TopAttachmentWorld;
        var to = new GameObject("To").transform;
        to.SetParent(b.transform, false);
        to.position = b.TopAttachmentWorld;
        span.Configure(from, to);
        span.tensionLemma = a.tensionLemma;
        return span;
    }

    void ClearGenerated()
    {
        poles.Clear();
        spans.Clear();
        if (polesRoot == null) return;
        for (int i = polesRoot.childCount - 1; i >= 0; i--)
        {
            var child = polesRoot.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
#endif
                Destroy(child);
        }
    }
}
