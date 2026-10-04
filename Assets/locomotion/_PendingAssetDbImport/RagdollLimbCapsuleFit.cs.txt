using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Per-limb capsule pose that does not require Scene/3D gizmo editing.
/// Hosts the CapsuleCollider on a child proxy so rotation offsets can be arbitrary
/// (Unity CapsuleCollider direction is otherwise limited to local X/Y/Z).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Ragdoll/Limb Capsule Fit")]
public sealed class RagdollLimbCapsuleFit : MonoBehaviour
{
    public const string ProxyName = "LimbCapsuleProxy";

    /// <summary>
    /// When true, <see cref="OnValidate"/> will not schedule/apply (editor AutoWire/Repair sets this
    /// around <c>AddComponent</c> so proxy creation is not run inside OnValidate).
    /// </summary>
    public static bool SuppressValidateApply;

    [Tooltip("Local translation of the capsule proxy relative to this bone.")]
    public Vector3 centerOffsetLocal;

    [Tooltip("Local euler degrees applied to the capsule proxy (e.g. 0,0,90 when a hand capsule points like a wristwatch).")]
    public Vector3 eulerOffsetDegrees;

    [Tooltip("Capsule axis on the proxy: 0=X, 1=Y, 2=Z.")]
    [Range(0, 2)] public int direction = 1;

    [Min(0.01f)] public float height = 0.12f;
    [Min(0.005f)] public float radius = 0.04f;

    [SerializeField] Transform proxy;
    [SerializeField] CapsuleCollider capsule;

    public Transform Proxy => proxy;
    public CapsuleCollider Capsule => capsule;

#if UNITY_EDITOR
    bool _applyQueued;
#endif

    void OnValidate()
    {
        if (!isActiveAndEnabled || SuppressValidateApply)
            return;

#if UNITY_EDITOR
        // Creating/destroying GOs or components inside OnValidate triggers SendMessage errors
        // and can leave proxies half-wired (unstable contact / wiggle after Repair).
        if (!Application.isPlaying)
        {
            if (_applyQueued)
                return;
            _applyQueued = true;
            EditorApplication.delayCall += DelayedApplyFromValidate;
            return;
        }
#endif
        Apply();
    }

#if UNITY_EDITOR
    void DelayedApplyFromValidate()
    {
        _applyQueued = false;
        if (this == null || SuppressValidateApply)
            return;
        Apply();
    }
#endif

    public void EnsureProxy()
    {
        if (proxy == null)
        {
            var existing = transform.Find(ProxyName);
            if (existing != null)
                proxy = existing;
        }

        if (proxy == null)
        {
            var go = new GameObject(ProxyName);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(go, "Create LimbCapsuleProxy");
#endif
            go.transform.SetParent(transform, false);
            proxy = go.transform;
        }

        if (capsule == null)
            capsule = proxy.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                capsule = Undo.AddComponent<CapsuleCollider>(proxy.gameObject);
            else
#endif
                capsule = proxy.gameObject.AddComponent<CapsuleCollider>();
        }

        // Collider must live on the proxy, not the bone (so rotation offsets work).
        var onBone = GetComponent<CapsuleCollider>();
        if (onBone != null && onBone != capsule)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(onBone);
            else
#endif
                Destroy(onBone);
        }
    }

    public void Apply()
    {
        EnsureProxy();
        if (proxy == null || capsule == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RecordObject(proxy, "Limb capsule fit");
            Undo.RecordObject(capsule, "Limb capsule fit");
        }
#endif
        proxy.localPosition = centerOffsetLocal;
        proxy.localRotation = Quaternion.Euler(eulerOffsetDegrees);
        proxy.localScale = Vector3.one;

        capsule.direction = Mathf.Clamp(direction, 0, 2);
        capsule.height = Mathf.Max(height, radius * 2f);
        capsule.radius = Mathf.Max(0.005f, radius);
        capsule.center = Vector3.zero;
    }

    public void RotateAxisDegrees(int axis, float degrees)
    {
        Vector3 e = eulerOffsetDegrees;
        if (axis == 0) e.x += degrees;
        else if (axis == 1) e.y += degrees;
        else e.z += degrees;
        eulerOffsetDegrees = e;
        Apply();
    }

    public void NudgeLocal(Vector3 delta)
    {
        centerOffsetLocal += delta;
        Apply();
    }
}
