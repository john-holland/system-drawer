using UnityEngine;

/// <summary>Authoritative pos/rot/scale for positional component lemmas via SharedDimensionalGenericCache.</summary>
[AddComponentMenu("Continuuuum/Dimensions/Dimensional Lemma Position")]
[DisallowMultipleComponent]
public sealed class DimensionalLemmaPosition : MonoBehaviour
{
    public bool applyScale;

    public void WriteTo(DimensionalPositionalSlot slot)
    {
        if (slot == null)
            return;
        slot.worldPos = transform.position;
        slot.worldRot = transform.rotation;
        slot.lossyScale = transform.lossyScale;
    }

    public void ApplyFrom(DimensionalPositionalSlot slot)
    {
        if (slot == null)
            return;
        transform.SetPositionAndRotation(slot.worldPos, slot.worldRot);
        if (applyScale && slot.lossyScale.sqrMagnitude > 1e-8f && transform.parent == null)
            transform.localScale = slot.lossyScale;
    }
}
