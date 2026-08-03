using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toilet overflow jet — hits ceiling, then roof if ceiling destroyed, then outdoor spout.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Toilet Overflow Jet")]
public sealed class ToiletOverflowJetDriver : MonoBehaviour
{
    public List<DestructibleLayerRef> layers = new List<DestructibleLayerRef>();
    public MonoBehaviour floodSimulator;
    public Transform jetOrigin;
    public float jetLitersPerSec = 0.8f;
    public bool jetActive;
    [Tooltip("When sky layer is active: 'at least the roof isn't wet'.")]
    public string skyModeCaption = "at least the roof isn't wet";
    [Tooltip("When still on ceiling: 'at least the ceiling isn't wet' is false — ceiling is wet.")]
    public string ceilingWetCaption = "ceiling wet";

    public DestructibleLayerKind CurrentTargetLayer { get; private set; } = DestructibleLayerKind.Ceiling;

    void Awake()
    {
        if (jetOrigin == null) jetOrigin = transform;
        EnsureDefaultLayers();
    }

    void EnsureDefaultLayers()
    {
        if (layers != null && layers.Count > 0) return;
        layers = new List<DestructibleLayerRef>
        {
            new DestructibleLayerRef { kind = DestructibleLayerKind.Ceiling },
            new DestructibleLayerRef { kind = DestructibleLayerKind.Roof },
            new DestructibleLayerRef { kind = DestructibleLayerKind.Sky }
        };
    }

    public void ActivateJet(float intensity01)
    {
        jetActive = intensity01 > 0.05f;
        if (!jetActive) return;
        ResolveTargetLayer();
        float lps = jetLitersPerSec * Mathf.Clamp01(intensity01);
        if (CurrentTargetLayer == DestructibleLayerKind.Sky)
            lps *= 1.25f; // outdoor spout
        Emit(lps);
    }

    public void ResolveTargetLayer()
    {
        EnsureDefaultLayers();
        // Ceiling intact → wet ceiling. Extra ceiling layers stay ceiling/spout-in-house by pressure handled upstream.
        for (int i = 0; i < layers.Count; i++)
        {
            var L = layers[i];
            if (L == null) continue;
            if (L.kind == DestructibleLayerKind.Sky)
            {
                CurrentTargetLayer = DestructibleLayerKind.Sky;
                return;
            }
            if (L.IsIntact())
            {
                CurrentTargetLayer = L.kind;
                return;
            }
        }
        CurrentTargetLayer = DestructibleLayerKind.Sky;
    }

    public void MarkLayerDestroyed(DestructibleLayerKind kind)
    {
        for (int i = 0; i < layers.Count; i++)
            if (layers[i] != null && layers[i].kind == kind)
                layers[i].destroyed = true;
        ResolveTargetLayer();
    }

    void Emit(float lps)
    {
        if (floodSimulator == null || lps <= 0f) return;
        floodSimulator.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) })
            ?.Invoke(floodSimulator, new object[] { lps });
    }
}
