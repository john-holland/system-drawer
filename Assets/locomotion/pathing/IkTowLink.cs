using System;
using UnityEngine;

/// <summary>
/// One parent→child link in a hierarchical IK tow chain.
/// Child tracks parent seat frame with stiffness; does not hitch as a single rigid attach.
/// </summary>
[Serializable]
public sealed class IkTowLink
{
    public string name = "tow";
    public Transform parent;
    public Transform child;
    public Rigidbody childBody;
    [Range(0f, 1f)] public float stiffness = 0.85f;
    public float maxErrorMeters = 0.35f;
    public Vector3 localOffsetFromParent;
    public bool useJointAssist;

    public Vector3 DesiredWorldPosition =>
        parent != null ? parent.TransformPoint(localOffsetFromParent) : localOffsetFromParent;

    public Vector3 CurrentWorldPosition =>
        child != null ? child.position : (childBody != null ? childBody.worldCenterOfMass : Vector3.zero);

    public float ErrorMeters => Vector3.Distance(CurrentWorldPosition, DesiredWorldPosition);

    /// <summary>
    /// Apply a corrective impulse/position blend so child tracks parent without full rigid hitch.
    /// </summary>
    public void Tick(float dt)
    {
        if (parent == null || (child == null && childBody == null))
            return;

        Vector3 desired = DesiredWorldPosition;
        Vector3 current = CurrentWorldPosition;
        Vector3 delta = desired - current;
        float err = delta.magnitude;
        if (err < 1e-5f)
            return;

        if (err > maxErrorMeters)
            delta = delta.normalized * maxErrorMeters;

        float k = Mathf.Clamp01(stiffness);
        Vector3 step = delta * Mathf.Clamp01(k * dt * 12f);

        if (childBody != null && !childBody.isKinematic)
        {
            float mass = Mathf.Max(0.1f, childBody.mass);
            childBody.AddForce(step * mass / Mathf.Max(dt, 1e-4f), ForceMode.Force);
            if (useJointAssist)
                childBody.MovePosition(Vector3.Lerp(current, desired, k * 0.15f));
        }
        else if (child != null)
        {
            child.position = Vector3.Lerp(current, desired, k * Mathf.Clamp01(dt * 10f));
        }
    }
}
