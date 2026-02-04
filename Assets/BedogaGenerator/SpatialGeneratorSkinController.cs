using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Policy for objects that are not in the current 4D/3D generator when switching skin.
/// </summary>
public enum SpatialSkinLooseObjectPolicy
{
    Disable,
    MoveToLimbo,
    ScaleToZero
}

/// <summary>
/// Controls which spatial generator skin is active. Holds skin list, active/editor indices,
/// applies skin (switch 3D/4D, stylesheet resolver, tree params), and optional loose-object policy.
/// Use with SpatialGenerator4DOrchestrator on same or parent GameObject.
/// </summary>
[ExecuteInEditMode]
public class SpatialGeneratorSkinController : MonoBehaviour
{
    [Header("Skins")]
    [Tooltip("Available skins. Each skin pairs a SpatialGenerator (3D) with a stylesheet and optional 4D.")]
    public List<SpatialGeneratorSkin> skins = new List<SpatialGeneratorSkin>();
    [Tooltip("Runtime active skin index. When skins are used, only this skin's 3D/4D are enabled.")]
    public int activeSkinIndex = 0;
    [Tooltip("Which skin to show in the editor when not playing. Applied in OnValidate and when dropdown changes.")]
    public int editorActiveSkinIndex = 0;

    [Header("References")]
    [Tooltip("Orchestrator that holds spatial generators. If null, resolved from parent or self.")]
    public SpatialGenerator4DOrchestrator orchestrator;

    [Header("Loose objects (not in current 3D/4D)")]
    [Tooltip("When switching skin at runtime, how to handle loose objects (e.g. move, hide, scale to zero).")]
    public SpatialSkinLooseObjectPolicy looseObjectPolicy = SpatialSkinLooseObjectPolicy.Disable;
    [Tooltip("Explicit list of loose GameObjects to move/hide when skin changes. Optional; can also use SpatialSkinLooseObject component.")]
    public List<GameObject> looseObjects = new List<GameObject>();
    [Tooltip("Optional parent transform for 'limbo' when looseObjectPolicy is MoveToLimbo. If null, a child is created.")]
    public Transform limboParent;

    [Header("Transition (Alice-style scale)")]
    [Tooltip("When true, ApplySkinWithTransition scales down the previous skin root and scales up the new skin root.")]
    public bool useScaleTransition = true;
    [Tooltip("Curve for transition (0 = start, 1 = end). Used to drive scale and progress events.")]
    public AnimationCurve transitionScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Optional shader global property name (e.g. _Dissolve or _Scale). When set, transition drives this float 0..1 so materials can dissolve or scale. Leave empty to skip.")]
    public string transitionShaderPropertyName = "";
    [Tooltip("When transitionShaderPropertyName is set, optionally limit which renderers get the property (empty = set globally via Shader.SetGlobalFloat).")]
    public List<Renderer> transitionShaderRenderers = new List<Renderer>();

    [Header("Transition events")]
    public UnityEvent onSkinTransitionStarted = new UnityEvent();
    public UnityEventFloat onSkinTransitionProgress = new UnityEventFloat();
    public UnityEvent onSkinTransitionCompleted = new UnityEvent();

    private bool transitionRunning;

    [System.Serializable]
    public class UnityEventFloat : UnityEvent<float> { }

    private void OnValidate()
    {
        if (!Application.isPlaying && skins != null && skins.Count > 0 && editorActiveSkinIndex >= 0 && editorActiveSkinIndex < skins.Count)
            ApplySkin(editorActiveSkinIndex);
    }

    private void Start()
    {
        ResolveOrchestrator();
        if (skins != null && skins.Count > 0 && activeSkinIndex >= 0 && activeSkinIndex < skins.Count)
            ApplySkin(activeSkinIndex);
    }

    private void ResolveOrchestrator()
    {
        if (orchestrator != null) return;
        orchestrator = GetComponent<SpatialGenerator4DOrchestrator>();
        if (orchestrator == null)
            orchestrator = GetComponentInParent<SpatialGenerator4DOrchestrator>();
    }

    /// <summary>Apply a skin by index. Enables that skin's 3D and optional 4D, disables others, sets prefab resolver and tree params. Optionally applies loose-object policy.</summary>
    public void ApplySkin(int index)
    {
        ResolveOrchestrator();
        if (skins == null || index < 0 || index >= skins.Count)
        {
            ClearResolverFromAllGenerators();
            return;
        }

        SpatialGeneratorSkin skin = skins[index];
        if (skin == null || skin.spatialGenerator3D == null)
        {
            ClearResolverFromAllGenerators();
            return;
        }

        // Disable all 3D and 4D in orchestrator list, then enable only this skin's
        if (orchestrator != null && orchestrator.spatialGenerators != null)
        {
            foreach (var gen in orchestrator.spatialGenerators)
            {
                if (gen == null) continue;
                bool isThisSkin3D = (gen == skin.spatialGenerator3D);
                bool isThisSkin4D = (skin.spatialGenerator4D != null && gen == skin.spatialGenerator4D);
                gen.Enabled = isThisSkin3D || isThisSkin4D;
            }
        }

        // Wire prefab resolver (from stylesheet) and tree params into the active 3D generator
        SpatialGenerator sg3d = skin.spatialGenerator3D;
        sg3d.SetPrefabResolverFromStylesheet(skin.stylesheet);
        Vector3 boundsScale = skin.EffectiveBoundsScale();
        float minCell = skin.EffectiveMinCellSize();
        int maxDepth = skin.EffectiveMaxDepth();
        int maxObjs = skin.EffectiveMaxObjectsPerNode();
        if (boundsScale != Vector3.one || minCell > 0f || maxDepth > 0 || maxObjs > 0)
            sg3d.ApplyTreeParamOverrides(boundsScale, minCell, maxDepth, maxObjs);

        // Loose objects: apply policy when in play mode (runtime skin switch)
        if (Application.isPlaying)
            ApplyLooseObjectPolicy();
    }

    private void ClearResolverFromAllGenerators()
    {
        if (orchestrator == null || orchestrator.spatialGenerators == null) return;
        foreach (var gen in orchestrator.spatialGenerators)
        {
            if (gen is SpatialGenerator sg3d)
                sg3d.SetPrefabResolverFromStylesheet((UnityEngine.Object)null);
        }
    }

    private void ApplyLooseObjectPolicy()
    {
        var toProcess = new List<GameObject>();
        if (looseObjects != null)
        {
            foreach (var go in looseObjects)
                if (go != null) toProcess.Add(go);
        }
        var looseComps = FindObjectsByType<SpatialSkinLooseObject>(FindObjectsSortMode.None);
        foreach (var c in looseComps)
            if (c != null && c.gameObject != null) toProcess.Add(c.gameObject);

        switch (looseObjectPolicy)
        {
            case SpatialSkinLooseObjectPolicy.Disable:
                foreach (var go in toProcess)
                    if (go != null) go.SetActive(false);
                break;
            case SpatialSkinLooseObjectPolicy.MoveToLimbo:
                if (limboParent == null)
                {
                    var limboGo = new GameObject("SpatialSkin_Limbo");
                    limboGo.transform.SetParent(transform);
                    limboParent = limboGo.transform;
                }
                foreach (var go in toProcess)
                    if (go != null) go.transform.SetParent(limboParent);
                break;
            case SpatialSkinLooseObjectPolicy.ScaleToZero:
                foreach (var go in toProcess)
                    if (go != null) go.transform.localScale = Vector3.zero;
                break;
        }
    }

    /// <summary>Apply skin with a transition (tween). Fires events and optionally scales current/new roots. Call from runtime.</summary>
    public void ApplySkinWithTransition(int toIndex, float duration)
    {
        if (transitionRunning || toIndex < 0 || toIndex >= (skins?.Count ?? 0))
            return;
        StartCoroutine(TransitionRoutine(toIndex, duration));
    }

    private IEnumerator TransitionRoutine(int toIndex, float duration)
    {
        transitionRunning = true;
        onSkinTransitionStarted?.Invoke();

        ApplySkin(toIndex);
        activeSkinIndex = toIndex;

        Transform newRoot = null;
        if (useScaleTransition && skins != null && toIndex < skins.Count)
        {
            var newSkin = skins[toIndex];
            if (newSkin != null && newSkin.spatialGenerator3D != null && newSkin.spatialGenerator3D.sceneTreeParent != null)
            {
                newRoot = newSkin.spatialGenerator3D.sceneTreeParent;
                newRoot.localScale = Vector3.zero;
            }
        }
        if (!string.IsNullOrEmpty(transitionShaderPropertyName))
        {
            if (transitionShaderRenderers != null && transitionShaderRenderers.Count > 0)
            {
                foreach (var r in transitionShaderRenderers)
                    if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(transitionShaderPropertyName))
                        r.material.SetFloat(transitionShaderPropertyName, 0f);
            }
            else
                Shader.SetGlobalFloat(Shader.PropertyToID(transitionShaderPropertyName), 0f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = transitionScaleCurve != null && transitionScaleCurve.keys.Length > 0 ? transitionScaleCurve.Evaluate(t) : t;
            onSkinTransitionProgress?.Invoke(curveT);
            if (newRoot != null)
                newRoot.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, curveT);
            if (!string.IsNullOrEmpty(transitionShaderPropertyName))
            {
                if (transitionShaderRenderers != null && transitionShaderRenderers.Count > 0)
                {
                    foreach (var r in transitionShaderRenderers)
                        if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(transitionShaderPropertyName))
                            r.material.SetFloat(transitionShaderPropertyName, curveT);
                }
                else
                    Shader.SetGlobalFloat(Shader.PropertyToID(transitionShaderPropertyName), curveT);
            }
            yield return null;
        }
        if (newRoot != null) newRoot.localScale = Vector3.one;
        if (!string.IsNullOrEmpty(transitionShaderPropertyName))
        {
            if (transitionShaderRenderers != null && transitionShaderRenderers.Count > 0)
            {
                foreach (var r in transitionShaderRenderers)
                    if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(transitionShaderPropertyName))
                        r.material.SetFloat(transitionShaderPropertyName, 1f);
            }
            else
                Shader.SetGlobalFloat(Shader.PropertyToID(transitionShaderPropertyName), 1f);
        }

        onSkinTransitionCompleted?.Invoke();
        transitionRunning = false;
    }

    /// <summary>Get the currently active skin (runtime or editor index depending on mode).</summary>
    public SpatialGeneratorSkin GetActiveSkin()
    {
        int idx = Application.isPlaying ? activeSkinIndex : editorActiveSkinIndex;
        if (skins == null || idx < 0 || idx >= skins.Count) return null;
        return skins[idx];
    }
}
