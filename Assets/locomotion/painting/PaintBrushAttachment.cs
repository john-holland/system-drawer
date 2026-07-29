using UnityEngine;

/// <summary>
/// Binds a paint brush (ferrule) to a ragdoll hand with authored grip offset.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Brush Attachment")]
public sealed class PaintBrushAttachment : MonoBehaviour
{
    public RagdollSystem ragdoll;
    public Transform brushRoot;
    public Transform ferrule;
    public string attachBoneName = "RightHand";
    public Vector3 localPositionOffset;
    public Vector3 localEulerOffset;
    public bool followHand = true;

    Transform _attach;

    void Awake()
    {
        if (ragdoll == null)
            ragdoll = GetComponentInParent<RagdollSystem>();
        if (ferrule == null && brushRoot != null)
            ferrule = brushRoot;
    }

    void LateUpdate()
    {
        if (!followHand || brushRoot == null) return;
        Transform bone = ResolveAttach();
        if (bone == null) return;
        Quaternion rot = bone.rotation * Quaternion.Euler(localEulerOffset);
        Vector3 pos = bone.TransformPoint(localPositionOffset);
        brushRoot.SetPositionAndRotation(pos, rot);
    }

    public void Attach(Transform brush, string boneName = null)
    {
        brushRoot = brush;
        if (ferrule == null)
            ferrule = brush;
        if (!string.IsNullOrEmpty(boneName))
            attachBoneName = boneName;
        _attach = null;
        followHand = true;
    }

    public void Detach()
    {
        followHand = false;
        brushRoot = null;
        _attach = null;
    }

    Transform ResolveAttach()
    {
        if (_attach != null) return _attach;
        if (ragdoll == null) return null;
        _attach = ragdoll.GetBoneTransform(attachBoneName);
        return _attach;
    }
}
