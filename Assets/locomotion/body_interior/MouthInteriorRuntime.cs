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
    public Renderer upperGumRenderer;
    public Renderer lowerGumRenderer;
    public Renderer mouthSkinRenderer;
    static readonly int GumHeightId = Shader.PropertyToID("_GumHeightMap");
    static readonly int GumScaleId = Shader.PropertyToID("_GumHeightScale");
    static readonly int FissureId = Shader.PropertyToID("_GumFissures");
    static readonly int DitherId = Shader.PropertyToID("_DitherMouthSkin");

    [Header("Jaw")]
    public Transform jawBone;
    [Tooltip("Optional authored kiss / lip-midpoint override (world pose driven by GetLipLoopMidpointWorld).")]
    public Transform lipMidOverride;
    [Tooltip("Vertical bite open (0 closed … 1 open).")]
    public float jawOpen01;
    [Tooltip("Molar 3D roll degrees (applied as yaw/roll on jaw).")]
    public float jawRollDeg;
    [Tooltip("Molar lateral shift toward preferred chew side.")]
    public float jawLateral01;
    public float maxJawOpenDegrees = 28f;
    public float maxJawRollDegrees = 8f;

    [Header("Interior")]
    public MouthExteriorEdgeLoop salivaLoop;
    public TongueRuntime tongue;
    public LipEdgeWrapDriver lipWrap;
    public DeveloperRespectsSeed seed;
    public float foodInMouthRadius;
    public ParticleSystem foodPresenceParticles;
    public bool useParticleFoodPresence = true;
    public Transform foodPresenceMesh;
    public bool spawnToothVisuals = true;
    public Mesh defaultToothMesh;
    public Material defaultToothMaterial;

    [Header("Mouthfeel")]
    public float mouthfeelLongevityRemaining;
    public float mouthfeelLongevityInitial;
    public float tasteIntensity01 = 1f;

    readonly List<Transform> _toothVisuals = new List<Transform>();
    MaterialPropertyBlock _gumBlock;
    Transform _lipMidRuntimeAnchor;

    public bool PreferRightChewSide => seed != null && seed.PreferRightSide;

    /// <summary>
    /// Middle of the lip loop — default kiss target. Prefers authored override, then saliva/rim loop,
    /// then front upper/lower arch average, then jaw / this transform.
    /// </summary>
    public Vector3 GetLipLoopMidpointWorld()
    {
        if (lipMidOverride != null)
            return lipMidOverride.position;
        if (salivaLoop == null)
            salivaLoop = GetComponentInChildren<MouthExteriorEdgeLoop>();
        if (salivaLoop != null)
            return salivaLoop.CenterWorld;

        EnsureDefaultTeeth();
        Vector3 sum = Vector3.zero;
        int n = 0;
        if (teeth != null)
        {
            for (int i = 0; i < teeth.Count; i++)
            {
                var s = teeth[i];
                if (s == null || !s.present || s.zone != ToothZone.Front) continue;
                sum += ResolveToothWorld(s);
                n++;
            }
        }
        if (n > 0)
            return sum / n;
        if (jawBone != null)
            return jawBone.position;
        return transform.position;
    }

    /// <summary>Ensures a Transform at the lip midpoint for IkTow / kiss anchors.</summary>
    public Transform EnsureLipMidAnchor()
    {
        if (lipMidOverride != null)
            return lipMidOverride;
        if (_lipMidRuntimeAnchor == null)
        {
            var existing = transform.Find("LipMidAnchor");
            if (existing != null)
                _lipMidRuntimeAnchor = existing;
            else
            {
                var go = new GameObject("LipMidAnchor");
                go.transform.SetParent(transform, false);
                _lipMidRuntimeAnchor = go.transform;
            }
        }
        _lipMidRuntimeAnchor.position = GetLipLoopMidpointWorld();
        return _lipMidRuntimeAnchor;
    }

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
        if (upperArchRoot == null)
            upperArchRoot = EnsureChild("UpperArch");
        if (lowerArchRoot == null)
            lowerArchRoot = EnsureChild("LowerArch");
        BindGumMaterials();
        if (spawnToothVisuals)
            RebuildToothVisuals();
    }

    void LateUpdate()
    {
        ApplyJawPose();
        UpdateFoodPresenceVisual();
        if (_lipMidRuntimeAnchor != null && lipMidOverride == null)
            _lipMidRuntimeAnchor.position = GetLipLoopMidpointWorld();
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

    Transform EnsureChild(string name)
    {
        var t = transform.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    public void EnsureDefaultTeeth()
    {
        if (teeth == null) teeth = new List<ToothSlot>();
        if (teeth.Count == 0)
            teeth.AddRange(ToothCatalog.BuildDefaultAdultSet());
    }

    public void BindGumMaterials()
    {
        _gumBlock ??= new MaterialPropertyBlock();
        ApplyGumTo(upperGumRenderer, upperGumHeightMap, upperGumFissures);
        ApplyGumTo(lowerGumRenderer, lowerGumHeightMap, lowerGumFissures);
        if (mouthSkinRenderer != null)
        {
            mouthSkinRenderer.GetPropertyBlock(_gumBlock);
            _gumBlock.SetFloat(DitherId, ditherMouthSkin ? 1f : 0f);
            mouthSkinRenderer.SetPropertyBlock(_gumBlock);
        }
    }

    void ApplyGumTo(Renderer r, Texture2D height, Texture2D fissure)
    {
        if (r == null) return;
        r.GetPropertyBlock(_gumBlock);
        if (height != null) _gumBlock.SetTexture(GumHeightId, height);
        if (fissure != null) _gumBlock.SetTexture(FissureId, fissure);
        _gumBlock.SetFloat(GumScaleId, gumHeightScale);
        r.SetPropertyBlock(_gumBlock);
    }

    public void RebuildToothVisuals()
    {
        EnsureDefaultTeeth();
        for (int i = 0; i < _toothVisuals.Count; i++)
        {
            if (_toothVisuals[i] != null)
                DestroyImmediateSafe(_toothVisuals[i].gameObject);
        }
        _toothVisuals.Clear();

        for (int i = 0; i < teeth.Count; i++)
        {
            var slot = teeth[i];
            if (slot == null || !slot.present) continue;
            Transform parent = slot.arch == ToothArch.Upper ? upperArchRoot : lowerArchRoot;
            var go = new GameObject($"Tooth_{slot.arch}_{slot.side}_{slot.kind}");
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = ResolveToothWorld(slot);
            go.transform.localScale = Vector3.one * ToothScale(slot.kind);

            if (slot.staticMesh != null || defaultToothMesh != null)
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = slot.staticMesh != null ? slot.staticMesh : defaultToothMesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (defaultToothMaterial != null)
                    mr.sharedMaterial = defaultToothMaterial;
            }
            else
            {
                // Placeholder capsule when no mesh authored.
                var prim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                prim.name = "ToothPrim";
                prim.transform.SetParent(go.transform, false);
                prim.transform.localScale = new Vector3(0.4f, 0.55f, 0.4f);
                Object.Destroy(prim.GetComponent<Collider>());
            }

            // SDF composition is referenced for editor/SDF Max tools; runtime uses mesh/prim.
            _ = slot.sdfComposition;
            _toothVisuals.Add(go.transform);
        }
    }

    static float ToothScale(ToothKind kind)
    {
        switch (kind)
        {
            case ToothKind.CentralIncisor: return 0.008f;
            case ToothKind.LateralIncisor: return 0.007f;
            case ToothKind.Canine: return 0.009f;
            case ToothKind.Wisdom: return 0.01f;
            default: return 0.011f;
        }
    }

    static void DestroyImmediateSafe(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Object.Destroy(go);
        else Object.DestroyImmediate(go);
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

    /// <summary>Buccal (+out), lingual (+in), occlusal (+biting) normals in world space for a tooth.</summary>
    public void ResolveToothFaceNormals(ToothSlot slot, out Vector3 buccal, out Vector3 lingual, out Vector3 occlusal)
    {
        Vector3 outward = transform.right;
        if (slot != null && slot.side == ToothSide.Left) outward = -transform.right;
        else if (slot != null && slot.side == ToothSide.Right) outward = transform.right;
        buccal = outward.normalized;
        lingual = (-outward).normalized;
        occlusal = slot != null && slot.arch == ToothArch.Upper ? -transform.up : transform.up;
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
        b.Expand(0.01f);
        return b;
    }

    /// <summary>Front bite: vertical open/close. Clears molar roll.</summary>
    public void DriveFrontBite(float open01)
    {
        jawOpen01 = Mathf.Clamp01(open01);
        jawRollDeg = 0f;
        jawLateral01 = 0f;
    }

    /// <summary>Molar chew: open + 3D roll toward preferred side.</summary>
    public void DriveMolarRoll(float phase01, bool preferRight)
    {
        float u = Mathf.Clamp01(phase01);
        jawOpen01 = 0.28f + 0.18f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f));
        jawRollDeg = Mathf.Sin(u * Mathf.PI * 4f) * maxJawRollDegrees * (preferRight ? 1f : -1f);
        jawLateral01 = (preferRight ? 1f : -1f) * (0.35f + 0.25f * Mathf.Sin(u * Mathf.PI * 2f));
    }

    public void ApplyJawPose()
    {
        if (jawBone == null) return;
        float pitch = -jawOpen01 * maxJawOpenDegrees;
        float roll = jawRollDeg;
        float yaw = jawLateral01 * maxJawRollDegrees * 0.5f;
        jawBone.localRotation = Quaternion.Euler(pitch, yaw, roll);
        if (lowerArchRoot != null && jawBone != lowerArchRoot)
            lowerArchRoot.localRotation = Quaternion.Euler(pitch * 0.85f, yaw * 0.5f, roll * 0.5f);
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
        EnsureFoodMesh();
        UpdateFoodPresenceVisual();
    }

    public void ClearFoodInMouth()
    {
        foodInMouthRadius = 0f;
        if (foodPresenceParticles != null && foodPresenceParticles.isPlaying)
            foodPresenceParticles.Stop();
        if (foodPresenceMesh != null)
            foodPresenceMesh.gameObject.SetActive(false);
    }

    void EnsureFoodMesh()
    {
        if (useParticleFoodPresence || foodPresenceMesh != null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FoodPresenceMesh";
        go.transform.SetParent(transform, false);
        Object.Destroy(go.GetComponent<Collider>());
        foodPresenceMesh = go.transform;
    }

    void UpdateFoodPresenceVisual()
    {
        if (useParticleFoodPresence || foodPresenceMesh == null) return;
        bool on = foodInMouthRadius > 1e-4f;
        foodPresenceMesh.gameObject.SetActive(on);
        if (!on) return;
        Vector3 pocket = tongue != null ? tongue.FoodPocketWorld : transform.position + transform.forward * 0.02f;
        foodPresenceMesh.position = pocket;
        foodPresenceMesh.localScale = Vector3.one * (foodInMouthRadius * 2f);
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
