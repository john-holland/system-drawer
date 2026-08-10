using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One limb's procedural placement along a parkour fall (local offset curves × fall progress).</summary>
[Serializable]
public sealed class ParkourFallLimbSlot
{
    public string limbId = "LeftHand";
    public Transform target;
    [Tooltip("Local offset from body at t=0 (start of fall).")]
    public Vector3 startLocalOffset = new Vector3(-0.3f, 0.2f, 0.1f);
    [Tooltip("Local offset from body at t=1 (contact / roll).")]
    public Vector3 endLocalOffset = new Vector3(-0.35f, -0.9f, 0.35f);
    [Tooltip("Blend start→end local offset (0-1 fall progress).")]
    public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Optional extra vertical bob along fall.")]
    public AnimationCurve heightCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    public float heightScale = 0.15f;

    public Vector3 SampleLocal(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        float u = blendCurve != null && blendCurve.keys != null && blendCurve.keys.Length > 0
            ? Mathf.Clamp01(blendCurve.Evaluate(t01))
            : t01;
        Vector3 p = Vector3.Lerp(startLocalOffset, endLocalOffset, u);
        float h = heightCurve != null && heightCurve.keys != null && heightCurve.keys.Length > 0
            ? heightCurve.Evaluate(t01)
            : 0f;
        p.y += h * heightScale;
        return p;
    }
}

/// <summary>Procedural fall animation curve + default limb placement slots for parkour fall BT.</summary>
[Serializable]
public sealed class ParkourFallProceduralCurve
{
    [Tooltip("Normalized fall progress envelope (visual / IK pace).")]
    public AnimationCurve fallProgress = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Body drop along world up (meters, negative = down).")]
    public AnimationCurve bodyDropMeters = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.55f, -0.4f),
        new Keyframe(1f, -1.1f));
    [Tooltip("Forward travel along body forward during fall.")]
    public AnimationCurve forwardMeters = AnimationCurve.Linear(0f, 0f, 1f, 0.8f);
    public List<ParkourFallLimbSlot> limbs = new List<ParkourFallLimbSlot>();

    public float EvaluateProgress(float rawT01)
    {
        rawT01 = Mathf.Clamp01(rawT01);
        if (fallProgress == null || fallProgress.keys == null || fallProgress.keys.Length == 0)
            return rawT01;
        return Mathf.Clamp01(fallProgress.Evaluate(rawT01));
    }

    public void EnsureDefaultLimbs()
    {
        if (limbs == null) limbs = new List<ParkourFallLimbSlot>();
        if (limbs.Count > 0) return;
        limbs.Add(new ParkourFallLimbSlot
        {
            limbId = "LeftHand",
            startLocalOffset = new Vector3(-0.35f, 0.15f, 0.2f),
            endLocalOffset = new Vector3(-0.4f, -0.85f, 0.45f)
        });
        limbs.Add(new ParkourFallLimbSlot
        {
            limbId = "RightHand",
            startLocalOffset = new Vector3(0.35f, 0.15f, 0.2f),
            endLocalOffset = new Vector3(0.4f, -0.85f, 0.45f)
        });
        limbs.Add(new ParkourFallLimbSlot
        {
            limbId = "LeftFoot",
            startLocalOffset = new Vector3(-0.15f, -0.95f, 0.05f),
            endLocalOffset = new Vector3(-0.2f, -1.05f, 0.55f)
        });
        limbs.Add(new ParkourFallLimbSlot
        {
            limbId = "RightFoot",
            startLocalOffset = new Vector3(0.15f, -0.95f, 0.05f),
            endLocalOffset = new Vector3(0.2f, -1.05f, 0.55f)
        });
    }
}
