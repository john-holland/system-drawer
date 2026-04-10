using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Readonly mirror of one 4D volume for the orchestrator's 4D tree mirror. Data is copyable; optional link to narrative tree for "Open in tree editor".
/// </summary>
public class Spatial4DMirrorNode : MonoBehaviour
{
    [Header("4D Bounds (readonly)")]
    public Vector3 center;
    public Vector3 size = Vector3.one;
    public float tMin;
    public float tMax;

    [Header("Payload")]
    [Tooltip("Label from the placed volume (e.g. Start, Stop).")]
    public string payloadLabel;

    [Header("Gateway termini (Back=tMin, Pause=centerT, Forward=tMax)")]
    public string causalityLeafBack;
    public string causalityLeafPause;
    public string causalityLeafForward;
    /// <summary>Same as pause leaf; aligns with legacy single causality_leaf_id.</summary>
    public string causalityLeafBase;

    [Header("Optional tree link")]
    [Tooltip("When set, inspector can show Open in tree editor.")]
    public MonoBehaviour narrativeTreeAsset;

    public Bounds4 Bounds4Value => new Bounds4(center, size, tMin, tMax);

    public void SetFrom(Bounds4 volume, string label)
    {
        SetFrom(volume, label, null);
    }

    public void SetFrom(Bounds4 volume, string label, Spatial4DTerminusTriplet gateway)
    {
        center = volume.center;
        size = volume.size;
        tMin = volume.tMin;
        tMax = volume.tMax;
        payloadLabel = label ?? "";
        if (gateway != null)
        {
            causalityLeafBack = gateway.back != null ? gateway.back.causalityLeafId : null;
            causalityLeafPause = gateway.pause != null ? gateway.pause.causalityLeafId : null;
            causalityLeafForward = gateway.forward != null ? gateway.forward.causalityLeafId : null;
        }
        else
        {
            causalityLeafBack = causalityLeafPause = causalityLeafForward = null;
        }
        causalityLeafBase = causalityLeafPause;
    }
}
