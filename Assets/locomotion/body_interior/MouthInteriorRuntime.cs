using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actor mouth: upper/lower 3D tooth splines, gum maps, saliva loop, food-in-mouth sphere, preferred chew side.
/// </summary>
[AddComponentMenu("Locomotion/Body Interior/Mouth Interior Runtime")]
public sealed class MouthInteriorRuntime : MonoBehaviour
{
    public List<ToothSlot> teeth = new List<ToothSlot>();
    public AnimationCurve upperArchSpline = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    public AnimationCurve lowerArchSpline = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    public Transform upperArchRoot;
    public Transform lowerArchRoot;
    public float archWidth = 0.05f;
    public float archDepth = 0.04f;

    [Header("Gums")]
    public Texture2D upperGumHeightMap;
    public Texture2D lowerGumHeightMap;
    public Texture2D upperGumFissures;
    public Texture2D lowerGumFissures;
    [Range(0.1f, 2f)] public float gumHeightScale = 1f;
    public bool ditherMouthSkin = true;

    [Header("Interior")]
    public MouthExteriorEdgeLoop salivaLoop;
    public TongueRuntime tongue;
    public LipEdgeWrapDriver lipWrap;
    public DeveloperRespectsSeed seed;
    public float foodInMouthRadius;
    public float jawOpen01;
    public ParticleSystem foodPresenceParticles;
    public bool useParticleFoodPresence = true;

    [Header("Mouthfeel")]
    public float mouthfeelLongevityRemaining;
    public float mouthfeelLongevityInitial;
    public float tasteIntensity01 = 1f;

    public bool PreferRightChewSide => seed != null && seed.PreferRightSide;

    void Awake()
    {
        if (teeth == null || teeth.Count == 0)
            teeth = new List<ToothSlot>(ToothCatalog.BuildDefaultAdultSet());
        if (seed == null)
            seed = DeveloperRespectsSeed.FindOrCreate(gameObject);
        else
            seed.EnsureResolved();
        if (salivaLoop == null)
            salivaLoop = GetComponentInChildren<MouthExteriorEdgeLoop>();
        if (tongue == null)
            tongue = GetComponentInChildren<TongueRuntime>();
        if (lipWrap == null)
            lipWrap = GetComponentInChildren<LipEdgeWrapDriver>();
    }

    void FixedUpdate()
    {
        salivaLoop?.TickSaliva(Time.fixedDeltaTime);
        if (mouthfeelLongevityRemaining > 0f)
        {
            mouthfeelLongevityRemaining = Mathf.Max(0f, mouthfeelLongevityRemaining - Time.fixedDeltaTime);
            tasteIntensity01 = mouthfeelLongevityInitial > 1e-4f
                ? Mathf.Clamp01(mouthfeelLongevityRemaining / mouthfeelLongevityInitial)
                : 0f;
        }
    }

    public void EnsureDefaultTeeth()
    {
        if (teeth == null) teeth = new List<ToothSlot>();
        if (teeth.Count == 0)
            teeth.AddRange(ToothCatalog.BuildDefaultAdultSet());
    }

    public Vector3 EvaluateArchPoint(ToothArch arch, float stop01, ToothSide side)
    {
        float t = Mathf.Clamp01(stop01);
        float lateral = side == ToothSide.Left ? -1f : (side == ToothSide.Right ? 1f : 0f);
        float curveY = arch == ToothArch.Upper ? upperArchSpline.Evaluate(t) : lowerArchSpline.Evaluate(t);
        Transform root = arch == ToothArch.Upper ? upperArchRoot : lowerArchRoot;
        Vector3 local = new Vector3(lateral * archWidth * (0.3f + t), curveY * 0.01f, Mathf.Lerp(archDepth, -archDepth * 0.2f, t));
        if (root != null)
            return root.TransformPoint(local);
        return transform.TransformPoint(local);
    }

    public Vector3 ResolveToothWorld(ToothSlot slot)
    {
        if (slot == null) return transform.position;
        Vector3 p = EvaluateArchPoint(slot.arch, slot.stop01, slot.side);
        Transform root = slot.arch == ToothArch.Upper ? upperArchRoot : lowerArchRoot;
        if (root != null)
            p += root.TransformVector(slot.biteOffset);
        else
            p += transform.TransformVector(slot.biteOffset);
        return p;
    }

    public Bounds FrontTeethExposureEllipsoid()
    {
        EnsureDefaultTeeth();
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        int n = 0;
        for (int i = 0; i < teeth.Count; i++)
        {
            var s = teeth[i];
            if (s == null || !s.present || s.zone != ToothZone.Front) continue;
            Vector3 p = ResolveToothWorld(s);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
            n++;
        }
        if (n == 0)
            return new Bounds(transform.position, Vector3.one * 0.03f);
        var b = new Bounds();
        b.SetMinMax(min, max);
        // Half-hemisphere ellipsoid: expand slightly for bite fit.
        b.Expand(0.01f);
        return b;
    }

    public void SetFoodInMouth(float radius, float mouthfeelSeconds)
    {
        foodInMouthRadius = Mathf.Max(0f, radius);
        mouthfeelLongevityInitial = Mathf.Max(0f, mouthfeelSeconds);
        mouthfeelLongevityRemaining = mouthfeelLongevityInitial;
        tasteIntensity01 = 1f;
        if (useParticleFoodPresence && foodPresenceParticles != null)
        {
            var main = foodPresenceParticles.main;
            main.startSize = foodInMouthRadius * 2f;
            if (!foodPresenceParticles.isPlaying)
                foodPresenceParticles.Play();
        }
    }

    public void ClearFoodInMouth()
    {
        foodInMouthRadius = 0f;
        if (foodPresenceParticles != null && foodPresenceParticles.isPlaying)
            foodPresenceParticles.Stop();
    }

    public IEnumerable<ToothSlot> EnumeratePresent(ToothZone? zone = null)
    {
        EnsureDefaultTeeth();
        for (int i = 0; i < teeth.Count; i++)
        {
            var s = teeth[i];
            if (s == null || !s.present) continue;
            if (zone.HasValue && s.zone != zone.Value) continue;
            yield return s;
        }
    }
}
