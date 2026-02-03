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

    [Header("Optional tree link")]
    [Tooltip("When set, inspector can show Open in tree editor.")]
    public MonoBehaviour narrativeTreeAsset;

    public Bounds4 Bounds4Value => new Bounds4(center, size, tMin, tMax);

    public void SetFrom(Bounds4 volume, string label)
    {
        center = volume.center;
        size = volume.size;
        tMin = volume.tMin;
        tMax = volume.tMax;
        payloadLabel = label ?? "";
    }
}
