using UnityEngine;

/// <summary>
/// Holds reference to a carried Transform and attach bone; each frame updates carried position/rotation from bone.
/// Good sections that are carry type register the object with this component when executed.
/// Attach to the same GameObject as RagdollSystem or BehaviorTree.
/// </summary>
public class CarriedObjectAttachment : MonoBehaviour
{
    [Header("Carried Object (set at runtime by carry card)")]
    [Tooltip("Transform of the object currently being carried. Updated each frame from attach point.")]
    public Transform carriedTransform;

    [Tooltip("Bone/attach point name (e.g. RightHand). Empty = use defaultAttachBoneName or RightHand.")]
    public string attachBoneName = "";

    [Tooltip("Default bone when attachBoneName is not set (e.g. from card).")]
    public string defaultAttachBoneName = "RightHand";

    private RagdollSystem ragdoll;
    private Transform attachPoint;

    private void Awake()
    {
        ragdoll = GetComponent<RagdollSystem>();
        if (ragdoll == null)
            ragdoll = GetComponentInChildren<RagdollSystem>();
    }

    private void LateUpdate()
    {
        if (carriedTransform == null) return;

        Transform point = GetAttachPoint();
        if (point != null)
        {
            carriedTransform.position = point.position;
            carriedTransform.rotation = point.rotation;
        }
    }

    /// <summary>
    /// Get the current attach point transform (from ragdoll bone).
    /// </summary>
    public Transform GetAttachPoint()
    {
        if (ragdoll == null) return null;
        string name = string.IsNullOrEmpty(attachBoneName) ? defaultAttachBoneName : attachBoneName;
        if (attachPoint != null && attachPoint.name == name)
            return attachPoint;
        attachPoint = ragdoll.GetBoneTransform(name);
        return attachPoint;
    }

    /// <summary>
    /// Set the carried object and optional attach bone. Call when starting a carry section.
    /// </summary>
    public void SetCarried(Transform carried, string boneName = null)
    {
        carriedTransform = carried;
        attachBoneName = boneName ?? "";
        attachPoint = null;
    }

    /// <summary>
    /// Clear carried object. Call when dropping or ending carry.
    /// </summary>
    public void ClearCarried()
    {
        carriedTransform = null;
        attachPoint = null;
    }

    /// <summary>
    /// True if we have a carried object and it is still near the attach point (within threshold).
    /// Used by pleaseHold to detect "put down" and re-grasp.
    /// </summary>
    public bool IsHeld(float distanceThreshold = 0.15f)
    {
        if (carriedTransform == null) return false;
        Transform point = GetAttachPoint();
        if (point == null) return false;
        return Vector3.Distance(carriedTransform.position, point.position) <= distanceThreshold;
    }
}
