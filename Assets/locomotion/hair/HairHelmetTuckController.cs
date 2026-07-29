using System.Collections;
using UnityEngine;

/// <summary>
/// Radial helmet tuck: golden-ratio conic sweep into radial cache, lock max(hair, helmet),
/// gate physics on covered sectors, then signal topological open/close helmet.
/// </summary>
[AddComponentMenu("Locomotion/Hair/Helmet Tuck Controller")]
public sealed class HairHelmetTuckController : MonoBehaviour
{
    public HairPlumeConfig config;
    public HairPlumePhysicsDriver physicsDriver;
    public Transform scalpRoot;
    public Transform helmetRoot;
    [Tooltip("Optional open/close topology host for the hard helmet shell.")]
    public MonoBehaviour openCloseHost;
    public float secondsPerTuckFrame = 0.08f;
    public float centerAzimuth01;

    HairHelmetSectionCache _sectionCache;
    HairHelmetTuckBehaviorTree _tuckTree;
    Coroutine _tuckRoutine;
    bool _tucked;

    public HairHelmetSectionCache SectionCache => _sectionCache;
    public bool IsTucked => _tucked;

    void Awake()
    {
        if (physicsDriver == null)
            physicsDriver = GetComponent<HairPlumePhysicsDriver>();
        if (scalpRoot == null)
            scalpRoot = transform;
        EnsureSectionCache();
        if (physicsDriver != null)
            physicsDriver.helmetSectionCache = _sectionCache;
    }

    void OnDestroy()
    {
        _sectionCache?.Dispose();
    }

    void EnsureSectionCache()
    {
        int az = config != null ? config.azimuthBins : 64;
        int len = config != null ? config.lengthBins : 32;
        bool needsNew = _sectionCache == null;
        if (!needsNew && _sectionCache.MaskTexture != null)
            needsNew = _sectionCache.MaskTexture.width != az || _sectionCache.MaskTexture.height != len;
        if (needsNew)
        {
            _sectionCache?.Dispose();
            _sectionCache = new HairHelmetSectionCache(az, len);
        }
        if (config != null)
            _sectionCache.SetRimUvEdge(config.helmetRimUvEdge);
        if (physicsDriver != null)
            physicsDriver.helmetSectionCache = _sectionCache;
    }

    public void BuildTuckTreeHierarchy()
    {
        _tuckTree = new HairHelmetTuckBehaviorTree(config);
        _tuckTree.BuildHierarchy(transform);
    }

    [ContextMenu("Play Tuck Then Close Helmet")]
    public void PlayTuckThenClose()
    {
        if (_tuckRoutine != null)
            StopCoroutine(_tuckRoutine);
        _tuckRoutine = StartCoroutine(TuckThenCloseCoroutine());
    }

    [ContextMenu("Open Helmet And Release Hair")]
    public void OpenAndRelease()
    {
        if (_tuckRoutine != null)
        {
            StopCoroutine(_tuckRoutine);
            _tuckRoutine = null;
        }
        EnsureSectionCache();
        _sectionCache.ClearCoverage();
        physicsDriver?.SetPhysicsEnabled(true);
        _tucked = false;
        TryInvokeOpenClose(open: true);
    }

    IEnumerator TuckThenCloseCoroutine()
    {
        EnsureSectionCache();
        _tuckTree ??= new HairHelmetTuckBehaviorTree(config);
        float dt = Mathf.Max(0.01f, secondsPerTuckFrame);

        for (int i = 0; i < _tuckTree.frames.Length; i++)
        {
            var frame = _tuckTree.frames[i];
            // Inward sweep: start wide, converge by φ
            float frac = frame.radiusFraction01;
            _sectionCache.ApplyConicTuck(frac, centerAzimuth01);

            // Disable physics on newly covered sectors while tuck animates
            physicsDriver?.SetPhysicsEnabled(true); // still run uncovered
            // Per-azimuth gate is inside SectionCache used by physics driver

            yield return new WaitForSeconds(dt);
        }

        // Full cover + height lock
        _sectionCache.ApplyConicTuck(1f, centerAzimuth01);
        float[] helmetInterior = SampleHelmetInteriorHeights();
        if (physicsDriver != null && physicsDriver.Cache != null)
            _sectionCache.CacheMaxHeight(physicsDriver.Cache, helmetInterior);

        // Covered sections: physics off for those azimuths (cache flags); global still on for pop-out
        _tucked = true;
        TryInvokeOpenClose(open: false);
        _tuckRoutine = null;
    }

    float[] SampleHelmetInteriorHeights()
    {
        EnsureSectionCache();
        int az = config != null ? config.azimuthBins : 64;
        int len = config != null ? config.lengthBins : 32;
        var heights = new float[az * len];
        float rim = config != null ? config.helmetRimUvEdge : 0.92f;
        float exteriorR = config != null ? config.tuckStartRadiusM : 0.22f;

        // Developer exterior radius closes hair: interior curve ~ conic shell height
        for (int v = 0; v < len; v++)
        {
            float length01 = v / (float)(len - 1);
            for (int u = 0; u < az; u++)
            {
                float azimuth01 = u / (float)az;
                float shell = Mathf.Clamp01(1f - length01 / Mathf.Max(1e-3f, rim));
                // Golden-ratio falloff from exterior setting radius
                float phiFall = 1f / HairPlumeConfig.GoldenRatio;
                float h = shell * Mathf.Clamp01(exteriorR / Mathf.Max(0.05f, config != null ? config.maxStrandLengthM : 0.35f)) * phiFall;
                if (helmetRoot != null)
                {
                    // Bias by helmet local up scale if present
                    h *= Mathf.Clamp(helmetRoot.lossyScale.y, 0.5f, 1.5f);
                }
                heights[v * az + u] = h * (0.85f + 0.15f * Mathf.Sin(azimuth01 * Mathf.PI * 2f));
            }
        }
        return heights;
    }

    void TryInvokeOpenClose(bool open)
    {
        if (openCloseHost == null) return;
        // Soft reflection so we do not hard-depend on open-close assembly types
        var type = openCloseHost.GetType();
        string method = open ? "RequestOpen" : "RequestClose";
        var mi = type.GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (mi != null && mi.GetParameters().Length == 0)
            mi.Invoke(openCloseHost, null);
    }

    void LateUpdate()
    {
        if (physicsDriver == null || physicsDriver.hairRenderer == null) return;
        EnsureSectionCache();
        var mat = physicsDriver.hairRenderer.material;
        _sectionCache.BindToMaterial(mat);
    }
}
